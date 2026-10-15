using Microsoft.Extensions.Options;

namespace BYPManagementSystem.Services
{
    // ============================================================
    // ATTENDANCE PHOTO STORAGE
    // ============================================================
    //
    // Saves attendance proof photos outside of wwwroot using
    // server-generated file names.
    //
    // Safety properties:
    // - File content is verified by magic bytes (not just the
    //   extension or the browser-supplied content type).
    // - File names are generated server-side, so client input
    //   can never influence the stored path.
    // - Reading/deleting re-sanitizes the name and verifies the
    //   resolved path stays inside the configured folder, which
    //   blocks path traversal.
    // - Photos are only served through controller actions that
    //   verify ownership, never through static file handling.
    //

    public class AttendancePhotoStorage
    {
        private const int HeaderLength = 12;


        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };


        private readonly IWebHostEnvironment _environment;
        private readonly AttendancePhotoOptions _options;


        public AttendancePhotoStorage(
            IWebHostEnvironment environment,
            IOptions<AttendancePhotoOptions> options)
        {
            _environment = environment;
            _options = options.Value;
        }


        public long MaxFileSizeBytes => _options.MaxFileSizeBytes;


        // ==========================================================
        // ROOT FOLDER
        // Content root + configured folder, fully resolved.
        // ==========================================================

        private string RootPath =>
            Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    _options.FolderPath));


        // ==========================================================
        // VALIDATE UPLOAD
        // Returns null when the upload is acceptable, otherwise a
        // user-facing error message.
        // ==========================================================

        public async Task<string?> ValidateAsync(
            IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Select an attendance photo to upload.";
            }


            if (file.Length > _options.MaxFileSizeBytes)
            {
                var megabytes =
                    Math.Round(
                        _options.MaxFileSizeBytes
                        / (1024d * 1024d),
                        1);

                return $"Attendance photos must be {megabytes} MB or smaller.";
            }


            var contentType =
                (file.ContentType ?? string.Empty)
                    .ToLowerInvariant();

            if (!AllowedContentTypes.Contains(contentType))
            {
                return "Upload a JPG, JPEG, PNG, or WEBP image.";
            }


            var header =
                new byte[HeaderLength];

            int read;

            await using (var stream = file.OpenReadStream())
            {
                read = await ReadHeaderAsync(
                    stream,
                    header);
            }


            if (DetectFormat(header, read) == null)
            {
                return "The selected file is not a valid JPEG, PNG, or WEBP image.";
            }


            return null;
        }


        // ==========================================================
        // SAVE
        // Returns the generated file name (no directory parts)
        // plus the content type detected from the file content.
        // ==========================================================

        public async Task<(string FileName, string ContentType)> SaveAsync(IFormFile file)
        {
            var header =
                new byte[HeaderLength];

            int read;

            await using (var stream = file.OpenReadStream())
            {
                stream.Position = 0;

                read = await ReadHeaderAsync(
                    stream,
                    header);
            }


            var extension =
                DetectFormat(header, read)
                ?? throw new InvalidOperationException(
                    "The file content is not a supported image format.");


            var fileName =
                $"{Guid.NewGuid():N}{extension}";


            var directory =
                RootPath;

            Directory.CreateDirectory(directory);


            await using (var stream = new FileStream(
                Path.Combine(directory, fileName),
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await file.CopyToAsync(stream);
            }


            return (fileName, ContentTypeForExtension(extension));
        }


        // ==========================================================
        // RESOLVE PATH
        // Sanitizes the stored name and guarantees the resolved
        // file stays inside the storage folder.
        // Returns null when the name is unsafe.
        // ==========================================================

        public string? GetFullPath(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }


            // Path.GetFileName strips any directory components,
            // including "../" traversal sequences.

            var safeName =
                Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeName) ||
                safeName != fileName)
            {
                return null;
            }


            var root =
                RootPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

            var fullPath =
                Path.GetFullPath(
                    Path.Combine(root, safeName));


            // Final defense: the resolved path must live inside
            // the storage folder.

            if (!fullPath.StartsWith(
                    root + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) &&
                !fullPath.StartsWith(
                    root + Path.AltDirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }


            return fullPath;
        }


        // ==========================================================
        // OPEN READ STREAM
        // Returns null when the file name is unsafe or the file
        // does not exist.
        // ==========================================================

        public Stream? OpenRead(string? fileName)
        {
            var path =
                GetFullPath(fileName);

            if (path == null || !File.Exists(path))
            {
                return null;
            }


            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }


        // ==========================================================
        // DELETE
        // Used when a saved file must be rolled back, and when a
        // permanently deleted record's file becomes safe to remove.
        // ==========================================================

        public void Delete(string? fileName)
        {
            var path =
                GetFullPath(fileName);

            if (path != null && File.Exists(path))
            {
                File.Delete(path);
            }
        }


        // ==========================================================
        // PENDING CLEANUP QUEUE (PERMANENT DELETE)
        // ==========================================================
        //
        // After a delete transaction has committed, removing the
        // stored photo file can still fail (file locked, transient
        // permissions problem). The bare file name is queued in a
        // small text file inside the storage folder and retried on
        // the next cleanup pass, so a failed removal stays
        // recoverable instead of silently leaking.
        //
        // Safety properties:
        // - Only validated bare file names are stored - never
        //   full paths supplied by anyone.
        // - Every retry resolves the name through GetFullPath
        //   again, so a tampered queue line can never escape the
        //   storage folder.
        // - The queue file itself can never be enqueued, and a
        //   line that fails validation is dropped without ever
        //   being touched on disk.
        // ==========================================================

        private static readonly object PendingQueueLock = new object();


        private string PendingQueuePath =>
            Path.Combine(
                RootPath,
                "cleanup-pending.txt");


        // ==========================================================
        // ENQUEUE A FILE NAME FOR A LATER DELETE RETRY
        // Best effort: failures here never break the caller,
        // because the database deletion has already committed.
        // ==========================================================

        public void EnqueuePendingDelete(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }


            var safeName =
                Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeName) ||
                safeName != fileName ||
                string.Equals(
                    safeName,
                    "cleanup-pending.txt",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            try
            {
                Directory.CreateDirectory(RootPath);

                lock (PendingQueueLock)
                {
                    File.AppendAllText(
                        PendingQueuePath,
                        safeName + Environment.NewLine);
                }
            }
            catch
            {
                // Queueing is best effort. The caller already
                // logged the original deletion failure.
            }
        }


        // ==========================================================
        // RETRY ALL QUEUED FILE DELETIONS
        // Returns how many names are still pending (0 = clean),
        // or -1 when the queue itself could not be processed.
        // Safe to call on every delete operation: names that no
        // longer exist, or that fail validation, are dropped.
        // ==========================================================

        public int RetryPendingDeletes()
        {
            lock (PendingQueueLock)
            {
                try
                {
                    if (!File.Exists(PendingQueuePath))
                    {
                        return 0;
                    }


                    var remaining =
                        new List<string>();

                    foreach (var raw in
                        File.ReadAllLines(PendingQueuePath))
                    {
                        var name =
                            (raw ?? string.Empty).Trim();

                        if (name.Length == 0)
                        {
                            continue;
                        }


                        var path =
                            GetFullPath(name);

                        if (path == null)
                        {
                            // Unsafe or malformed line: drop it
                            // without touching the disk.

                            continue;
                        }


                        try
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }


                            if (File.Exists(path))
                            {
                                remaining.Add(name);
                            }
                        }
                        catch
                        {
                            // Still failing: keep it queued for
                            // the next retry.

                            remaining.Add(name);
                        }
                    }


                    if (remaining.Count == 0)
                    {
                        File.Delete(PendingQueuePath);
                    }
                    else
                    {
                        File.WriteAllLines(
                            PendingQueuePath,
                            remaining);
                    }


                    return remaining.Count;
                }
                catch
                {
                    return -1;
                }
            }
        }


        // ==========================================================
        // CONTENT TYPE FOR SERVING
        // ==========================================================

        public static string ResolveContentType(
            string contentType)
        {
            var normalized =
                (contentType ?? string.Empty)
                    .ToLowerInvariant();

            return AllowedContentTypes.Contains(normalized)
                ? normalized
                : "application/octet-stream";
        }


        private static string ContentTypeForExtension(
            string extension) =>
            extension.ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };


        // ==========================================================
        // MAGIC BYTES
        // JPEG: FF D8 FF
        // PNG : 89 50 4E 47 0D 0A 1A 0A
        // WebP: "RIFF" .... "WEBP"
        // ==========================================================

        private static string? DetectFormat(
            byte[] header,
            int read)
        {
            if (read >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return ".jpg";
            }


            if (read >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                return ".png";
            }


            if (read >= 12 &&
                header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'E' &&
                header[10] == (byte)'B' &&
                header[11] == (byte)'P')
            {
                return ".webp";
            }


            return null;
        }


        private static async Task<int> ReadHeaderAsync(
            Stream stream,
            byte[] buffer)
        {
            var total = 0;

            while (total < buffer.Length)
            {
                var count = await stream.ReadAsync(
                    buffer.AsMemory(
                        total,
                        buffer.Length - total));

                if (count == 0)
                {
                    break;
                }

                total += count;
            }

            return total;
        }
    }
}
