$content = Get-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Attendance/Index.cshtml' -Raw

# Update program-panel
$content = $content -replace 'padding: 18px;', 'padding: 10px;'
$content = $content -replace 'border: 1px solid #e2e8f0;', 'border: 1px solid rgba(183,215,229,.14);'
$content = $content -replace 'border-radius: 13px;', 'border-radius: 11px;'
$content = $content -replace 'background: #ffffff;', 'background: linear-gradient(135deg, rgba(5,45,46,.82), rgba(3,23,22,.94));'
$content = $content -replace 'box-shadow: 0 7px 20px rgba\(15, 23, 42, .03\);', 'box-shadow: 0 6px 16px rgba(0,0,0,.08);'

# Update panel-header
$content = $content -replace 'margin-bottom: 15px;', 'margin-bottom: 8px;'
$content = $content -replace 'gap: 12px;', 'gap: 8px;'

# Update panel-eyebrow
$content = $content -replace 'font-size: 7px;', 'font-size: 6px;'
$content = $content -replace 'letter-spacing: 1px;', 'letter-spacing: .8px;'

# Update panel-header h2
$content = $content -replace 'font-size: .94rem;', 'font-size: 12px;'
$content = $content -replace 'margin: 4px 0 3px;', 'margin: 2px 0 1px;'

# Update panel-header p
$content = $content -replace 'font-size: .56rem;', 'font-size: 7px;'

# Update program-count
$content = $content -replace 'min-height: 28px;', 'min-height: 22px;'
$content = $content -replace 'font-size: .51rem;', 'font-size: 6px;'
$content = $content -replace 'background: #f8fafc;', 'background: rgba(105,199,232,.08);'
$content = $content -replace 'border: 1px solid #e2e8f0;', 'border: 1px solid rgba(105,199,232,.14);'
$content = $content -replace 'color: #64748b;', 'color: #8DDCF2;'

# Update program-grid
$content = $content -replace 'gap: 10px;', 'gap: 8px;'

# Update program-card
$content = $content -replace 'border: 1px solid #e6ebf0;', 'border: 1px solid rgba(183,215,229,.14);'
$content = $content -replace 'border-radius: 14px;', 'border-radius: 11px;'
$content = $content -replace 'background: #fbfcfe;', 'background: rgba(255,255,255,.025);'

# Update card-header
$content = $content -replace 'padding: 13px 13px 0;', 'padding: 8px 8px 0;'

# Update program-icon
$content = $content -replace 'width: 37px;', 'width: 30px;'
$content = $content -replace 'height: 37px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'
$content = $content -replace 'background: #eff6ff;', 'background: rgba(105,199,232,.10);'
$content = $content -replace 'color: #0d6efd;', 'color: #69DDF2;'
$content = $content -replace 'font-size: .8rem;', 'font-size: 12px;'

# Update status-badge
$content = $content -replace 'min-height: 22px;', 'min-height: 18px;'
$content = $content -replace 'padding: 0 7px;', 'padding: 0 5px;'
$content = $content -replace 'font-size: .49rem;', 'font-size: 6px;'
$content = $content -replace 'font-weight: 750;', 'font-weight: 700;'
$content = $content -replace 'background: #ecfdf5;', 'background: rgba(43,166,126,.12);'
$content = $content -replace 'color: #15803d;', 'color: #69F0AE;'
$content = $content -replace 'background: #f1f5f9;', 'background: rgba(183,215,229,.07);'
$content = $content -replace 'color: #64748b;', 'color: #A9BAC0;'

# Update status-dot
$content = $content -replace 'width: 5px;', 'width: 4px;'
$content = $content -replace 'height: 5px;', 'height: 4px;'

# Update program-body
$content = $content -replace 'padding: 12px 13px 13px;', 'padding: 8px 8px 8px;'

# Update program-body h3
$content = $content -replace 'font-size: .75rem;', 'font-size: 10px;'
$content = $content -replace 'margin: 0 0 6px;', 'margin: 0 0 4px;'

# Update description
$content = $content -replace 'min-height: 35px;', 'min-height: 28px;'
$content = $content -replace 'font-size: .55rem;', 'font-size: 8px;'

# Update detail-row
$content = $content -replace 'gap: 7px;', 'gap: 5px;'

# Update detail-icon
$content = $content -replace 'width: 27px;', 'width: 22px;'
$content = $content -replace 'height: 27px;', 'height: 22px;'
$content = $content -replace 'border-radius: 7px;', 'border-radius: 6px;'
$content = $content -replace 'background: #ffffff;', 'background: rgba(255,255,255,.05);'
$content = $content -replace 'border: 1px solid #e7ebf0;', 'border: 1px solid rgba(183,215,229,.10);'
$content = $content -replace 'color: #0d6efd;', 'color: #69DDF2;'
$content = $content -replace 'font-size: .58rem;', 'font-size: 8px;'

# Update detail-row span
$content = $content -replace 'font-size: .47rem;', 'font-size: 6px;'

# Update detail-row strong
$content = $content -replace 'font-size: .53rem;', 'font-size: 8px;'

# Update program-actions
$content = $content -replace 'padding: 0 13px 12px;', 'padding: 0 8px 8px;'
$content = $content -replace 'gap: 6px;', 'gap: 5px;'

# Update action-button
$content = $content -replace 'min-height: 31px;', 'min-height: 26px;'
$content = $content -replace 'padding: 0 7px;', 'padding: 0 5px;'
$content = $content -replace 'font-size: .51rem;', 'font-size: 6px;'
$content = $content -replace 'font-weight: 750;', 'font-weight: 700;'
$content = $content -replace 'border-radius: 7px;', 'border-radius: 6px;'
$content = $content -replace 'border: 1px solid #dfe6ee;', 'border: 1px solid rgba(183,215,229,.15);'
$content = $content -replace 'background: #ffffff;', 'background: rgba(255,255,255,.04);'
$content = $content -replace 'color: #64748b;', 'color: #8DDCF2;'
$content = $content -replace 'border: 1px solid #bfdbfe;', 'border: 1px solid rgba(105,199,232,.20);'
$content = $content -replace 'background: #eff6ff;', 'background: rgba(105,199,232,.08);'
$content = $content -replace 'color: #2563eb;', 'color: #69DDF2;'

# Update empty-state
$content = $content -replace 'min-height: 280px;', 'min-height: 90px;'

# Update empty-icon
$content = $content -replace 'width: 51px;', 'width: 36px;'
$content = $content -replace 'height: 51px;', 'height: 36px;'
$content = $content -replace 'font-size: 1rem;', 'font-size: 14px;'
$content = $content -replace 'background: #f1f5f9;', 'background: rgba(105,199,232,.09);'
$content = $content -replace 'color: #94a3b8;', 'color: #8DDCF2;'

# Update empty-state h3
$content = $content -replace 'font-size: .76rem;', 'font-size: 10px;'

# Update empty-state p
$content = $content -replace 'font-size: .57rem;', 'font-size: 7px;'
$content = $content -replace 'max-width: 360px;', 'max-width: 280px;'

# Update empty-button
$content = $content -replace 'min-height: 34px;', 'min-height: 26px;'
$content = $content -replace 'font-size: .54rem;', 'font-size: 6px;'
$content = $content -replace 'font-weight: 750;', 'font-weight: 700;'
$content = $content -replace 'background: #0d6efd;', 'background: #0C9096;'
$content = $content -replace 'color: #ffffff;', 'color: #052D2E;'

# Update bottom-actions
$content = $content -replace 'margin-top: 15px;', 'margin-top: 8px;'
$content = $content -replace 'gap: 8px;', 'gap: 6px;'

# Update bottom-button
$content = $content -replace 'min-height: 36px;', 'min-height: 26px;'
$content = $content -replace 'font-size: .55rem;', 'font-size: 6px;'
$content = $content -replace 'font-weight: 750;', 'font-weight: 700;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 6px;'
$content = $content -replace 'border: 1px solid #dfe6ee;', 'border: 1px solid rgba(183,215,229,.15);'
$content = $content -replace 'background: #ffffff;', 'background: rgba(255,255,255,.04);'
$content = $content -replace 'color: #64748b;', 'color: #8DDCF2;'

Set-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Attendance/Index.cshtml' $content