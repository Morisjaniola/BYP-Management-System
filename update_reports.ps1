$content = Get-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Reports/Index.cshtml' -Raw

# Update reports-page padding
$content = $content -replace 'padding:\s*18px 26px 24px;', 'padding: 12px 18px 18px;'

# Update page-header
$content = $content -replace 'margin-bottom: 20px;', 'margin-bottom: 12px;'
$content = $content -replace 'gap: 20px;', 'gap: 14px;'

# Update page-eyebrow
$content = $content -replace 'font-size: 9px;', 'font-size: 7px;'
$content = $content -replace 'letter-spacing: 1.5px;', 'letter-spacing: 1px;'

# Update page-header h1
$content = $content -replace 'font-size: 26px;', 'font-size: 20px;'
$content = $content -replace 'margin:\s*5px 0 4px;', 'margin: 3px 0 2px;'

# Update page-header p
$content = $content -replace 'font-size: 10px;', 'font-size: 8px;'
$content = $content -replace 'color: #64748b;', 'color: #C5DDE2;'

# Update header-button
$content = $content -replace 'min-height: 35px;', 'min-height: 30px;'
$content = $content -replace 'font-size: .54rem;', 'font-size: 7px;'
$content = $content -replace 'font-weight: 750;', 'font-weight: 700;'
$content = $content -replace 'border-radius: 7px;', 'border-radius: 7px;'

# Update header-button.secondary
$content = $content -replace 'border:\s*1px solid #dfe6ee;', 'border: 1px solid rgba(183,215,229,.16);'
$content = $content -replace 'background: #ffffff;', 'background: rgba(255,255,255,.06);'
$content = $content -replace 'color: #64748b;', 'color: #C5DDE2;'

# Update header-button.primary
$content = $content -replace 'border:\s*1px solid #0d6efd;', 'border: 1px solid #0C9096;'
$content = $content -replace 'background: #0d6efd;', 'background: #0C9096;'
$content = $content -replace 'color: #ffffff;', 'color: #FFFFFF;'

# Update summary-grid
$content = $content -replace 'margin-bottom: 15px;', 'margin-bottom: 8px;'
$content = $content -replace 'gap: 10px;', 'gap: 8px;'

# Update summary-card
$content = $content -replace 'padding:\s*11px 14px;', 'padding: 10px 11px;'
$content = $content -replace 'border-radius: 11px;', 'border-radius: 10px;'
$content = $content -replace 'border:\s*1px solid #e2e8f0;', 'border: 1px solid rgba(183,215,229,.13);'
$content = $content -replace 'background: #ffffff;', 'background: rgba(255,255,255,.035);'
$content = $content -replace 'box-shadow:\s*0 6px 18px rgba\(15, 23, 42, .025\);', 'box-shadow: 0 4px 16px rgba(31, 43, 70, .03);'

# Update summary-icon
$content = $content -replace 'width: 38px;', 'width: 34px;'
$content = $content -replace 'height: 38px;', 'height: 34px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 9px;'
$content = $content -replace 'font-size: .78rem;', 'font-size: 14px;'

# Update summary-icon colors (keep them but ensure they work in dark)
# These are light mode colors, they'll be overridden in dark mode

# Update summary-content span
$content = $content -replace 'font-size: 9px;', 'font-size: 6px;'
$content = $content -replace 'font-weight: 700;', 'font-weight: 700;'

# Update summary-content strong
$content = $content -replace 'font-size: 21px;', 'font-size: 16px;'
$content = $content -replace 'margin-top: 3px;', 'margin-top: 1px;'

# Update summary-content small
$content = $content -replace 'font-size: 8px;', 'font-size: 6px;'
$content = $content -replace 'margin-top: 4px;', 'margin-top: 2px;'

# Update report-panel
$content = $content -replace 'padding: 14px;', 'padding: 10px;'
$content = $content -replace 'border-radius: 14px;', 'border-radius: 11px;'
$content = $content -replace 'background: linear-gradient\(135deg, rgba\(5,45,46,.82\), rgba\(3,23,22,.94\)\);', 'background: linear-gradient(135deg, rgba(5,45,46,.82), rgba(3,23,22,.94));'
$content = $content -replace 'border: 1px solid rgba\(183,215,229,.14\);', 'border: 1px solid rgba(183,215,229,.14);'

# Update panel-header
$content = $content -replace 'min-height: 43px;', 'min-height: 34px;'
$content = $content -replace 'gap: 12px;', 'gap: 8px;'
$content = $content -replace 'margin-bottom: 11px;', 'margin-bottom: 8px;'

# Update panel-eyebrow
$content = $content -replace 'font-size: 7px;', 'font-size: 6px;'
$content = $content -replace 'letter-spacing: 1px;', 'letter-spacing: .8px;'

# Update panel-header h2
$content = $content -replace 'font-size: 15px;', 'font-size: 12px;'
$content = $content -replace 'margin: 3px 0 2px;', 'margin: 2px 0 1px;'

# Update panel-header p
$content = $content -replace 'font-size: 8px;', 'font-size: 7px;'

# Update panel-icon-box
$content = $content -replace 'width: 36px;', 'width: 30px;'
$content = $content -replace 'height: 36px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'

# Update panel-link
$content = $content -replace 'font-size: 8px;', 'font-size: 7px;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'

# Update panel-print-button
$content = $content -replace 'min-height: 28px;', 'min-height: 24px;'
$content = $content -replace 'padding: 0 9px;', 'padding: 0 7px;'
$content = $content -replace 'font-size: .5rem;', 'font-size: 5px;'
$content = $content -replace 'font-weight: 700;', 'font-weight: 700;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 6px;'
$content = $content -replace 'border: 1px solid #cbd5e1;', 'border: 1px solid rgba(183,215,229,.17);'
$content = $content -replace 'background: #ffffff;', 'background: rgba(255,255,255,.04);'
$content = $content -replace 'color: #475569;', 'color: #8DDCF2;'

# Update record-count
$content = $content -replace 'min-height: 26px;', 'min-height: 22px;'
$content = $content -replace 'padding: 0 9px;', 'padding: 0 7px;'
$content = $content -replace 'border-radius: 7px;', 'border-radius: 5px;'
$content = $content -replace 'background: #f1f5f9;', 'background: rgba(104,157,184,.08);'
$content = $content -replace 'color: #64748b;', 'color: #B7D7E5;'
$content = $content -replace 'font-size: .5rem;', 'font-size: 6px;'

# Update metric-grid
$content = $content -replace 'gap: 8px;', 'gap: 6px;'

# Update metric-card
$content = $content -replace 'padding: 11px;', 'padding: 9px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'
$content = $content -replace 'background: rgba\(255,255,255,.035\);', 'background: rgba(255,255,255,.035);'
$content = $content -replace 'border: 1px solid rgba\(183,215,229,.10\);', 'border: 1px solid rgba(183,215,229,.10);'

# Update metric-icon
$content = $content -replace 'width: 34px;', 'width: 28px;'
$content = $content -replace 'height: 34px;', 'height: 28px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'
$content = $content -replace 'background: rgba\(255,255,255,.07\);', 'background: rgba(255,255,255,.07);'
$content = $content -replace 'font-size: 14px;', 'font-size: 12px;'

# Update metric-card span
$content = $content -replace 'font-size: 7px;', 'font-size: 6px;'

# Update metric-card strong
$content = $content -replace 'font-size: 18px;', 'font-size: 14px;'
$content = $content -replace 'margin-top: 3px;', 'margin-top: 2px;'

# Update table-wrapper
$content = $content -replace 'border-radius: 10px;', 'border-radius: 8px;'
$content = $content -replace 'border: 1px solid rgba\(105,199,232,.20\);', 'border: 1px solid rgba(105,199,232,.20);'

# Update report-table th
$content = $content -replace 'height: 38px;', 'height: 32px;'
$content = $content -replace 'padding: 0 10px;', 'padding: 0 8px;'
$content = $content -replace 'font-size: 8px;', 'font-size: 7px;'
$content = $content -replace 'letter-spacing: .7px;', 'letter-spacing: .6px;'

# Update report-table td
$content = $content -replace 'padding: 9px 10px;', 'padding: 7px 8px;'
$content = $content -replace 'font-size: 8px;', 'font-size: 7px;'

# Update name-cell strong
$content = $content -replace 'font-size: 9px;', 'font-size: 8px;'

# Update mini-avatar
$content = $content -replace 'width: 30px;', 'width: 26px;'
$content = $content -replace 'height: 30px;', 'height: 26px;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 7px;'

# Update table-status
$content = $content -replace 'min-height: 21px;', 'min-height: 16px;'
$content = $content -replace 'padding: 0 7px;', 'padding: 0 5px;'
$content = $content -replace 'font-size: 6px;', 'font-size: 5px;'
$content = $content -replace 'font-weight: 700;', 'font-weight: 700;'

# Update empty-table
$content = $content -replace 'min-height: 220px;', 'min-height: 90px;'

# Update empty-icon (if present)
$content = $content -replace 'font-size: 19px;', 'font-size: 14px;'

# Update empty-table strong
$content = $content -replace 'font-size: 11px;', 'font-size: 10px;'

# Update empty-table span
$content = $content -replace 'font-size: 8px;', 'font-size: 7px;'

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
$content = $content -replace 'border: 1px solid #0d6efd;', 'border: 1px solid #0C9096;'
$content = $content -replace 'background: #0d6efd;', 'background: #0C9096;'
$content = $content -replace 'color: #ffffff;', 'color: #052D2E;'

# Update responsive
$content = $content -replace 'padding: 21px 17px 35px;', 'padding: 14px 14px 24px;'

Set-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Reports/Index.cshtml' $content