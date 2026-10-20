using BYPManagementSystem.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BYPManagementSystem.Tests.Infrastructure
{
    // =========================================================
    // ISOLATED WEB APPLICATION FACTORY
    // =========================================================
    // Starts the real BYProgram pipeline (routing, model
    // binding, anti-forgery, Identity, cookies) with the MySQL
    // provider replaced by a private EF Core in-memory database.
    //
    // Tests can never touch the real application database:
    // every factory instance uses a unique database name that
    // disappears when the factory is disposed.
    // =========================================================

    public class BypWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName =
            $"byp-tests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            // "Testing" skips the development seed accounts in
            // Program.cs while still creating the required roles.
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(
                services =>
                    ReplaceMySqlDatabase(
                        services,
                        _databaseName
                    )
            );
        }

        internal static void ReplaceMySqlDatabase(
            IServiceCollection services,
            string databaseName)
        {
            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseInMemoryDatabase(databaseName)
            );
        }
    }


    // =========================================================
    // FAILING-DATABASE TEST DOUBLE
    // =========================================================

    public sealed class DatabaseFailureState
    {
        public bool FailSaves { get; set; }
    }


    public sealed class FailingSaveChangesDbContext
        : ApplicationDbContext
    {
        private readonly DatabaseFailureState _failureState;

        public FailingSaveChangesDbContext(
            DbContextOptions<ApplicationDbContext> options,
            DatabaseFailureState failureState)
            : base(options)
        {
            _failureState = failureState;
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            if (_failureState.FailSaves)
            {
                throw new DbUpdateException(
                    "Simulated database failure for integration tests."
                );
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }


    // =========================================================
    // WEB APPLICATION FACTORY WITH A FAILING DATABASE
    // =========================================================
    // Startup seeding succeeds (FailSaves starts false); the
    // test flips the switch to simulate a database outage while
    // the registration POST is running.
    // =========================================================

    public sealed class FailingDatabaseFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName =
            $"byp-tests-fail-{Guid.NewGuid():N}";

        public DatabaseFailureState FailureState =>
            Services.GetRequiredService<DatabaseFailureState>();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<
                    DbContextOptions<ApplicationDbContext>>();

                services.RemoveAll<
                    IDbContextOptionsConfiguration<ApplicationDbContext>>();

                services.RemoveAll<ApplicationDbContext>();

                services.AddSingleton<DatabaseFailureState>();

                services.AddSingleton(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseInMemoryDatabase(_databaseName)
                        .Options
                );

                services.AddScoped<ApplicationDbContext>(
                    provider =>
                        new FailingSaveChangesDbContext(
                            provider.GetRequiredService<
                                DbContextOptions<ApplicationDbContext>>(),
                            provider.GetRequiredService<
                                DatabaseFailureState>()
                        )
                );
            });
        }
    }
}
