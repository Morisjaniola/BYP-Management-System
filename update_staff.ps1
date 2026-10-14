$content = Get-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Staff/Dashboard.cshtml' -Raw

# Update staff-dashboard-page padding
$content = $content -replace 'padding: 6px 4px 42px;', 'padding: 4px 3px 28px 3px;'

# Update dashboard-header
$content = $content -replace 'gap: 25px;', 'gap: 16px;'
$content = $content -replace 'margin-bottom: 20px;', 'margin-bottom: 18px;'

# Update dashboard-eyebrow
$content = $content -replace 'font-size: .69rem;', 'font-size: .58rem;'
$content = $content -replace 'letter-spacing: .15em;', 'letter-spacing: .1em;'

# Update dashboard-header h1
$content = $content -replace 'font-size: 2rem;', 'font-size: 1.5rem;'
$content = $content -replace 'margin: 6px 0 5px;', 'margin: 4px 0 3px;'

# Update dashboard-header p
$content = $content -replace 'font-size: .85rem;', 'font-size: .75rem;'

# Update staff-account (if present)
$content = $content -replace 'min-width: 230px;', 'min-width: 180px;'
$content = $content -replace 'padding: 11px 13px;', 'padding: 8px 10px;'
$content = $content -replace 'border-radius: 12px;', 'border-radius: 10px;'

# Update staff-avatar
$content = $content -replace 'width: 40px;', 'width: 34px;'
$content = $content -replace 'height: 40px;', 'height: 34px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.25rem;', 'font-size: 1rem;'

# Update staff-account span
$content = $content -replace 'font-size: .54rem;', 'font-size: .5rem;'
$content = $content -replace 'letter-spacing: .1em;', 'letter-spacing: .08em;'

# Update staff-account strong
$content = $content -replace 'font-size: .67rem;', 'font-size: .6rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update staff-account small
$content = $content -replace 'font-size: .57rem;', 'font-size: .52rem;'

# Update quick-actions
$content = $content -replace 'gap: 10px;', 'gap: 8px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'

# Update quick-action
$content = $content -replace 'min-height: 65px;', 'min-height: 52px;'
$content = $content -replace 'padding: 11px;', 'padding: 8px;'
$content = $content -replace 'border-radius: 12px;', 'border-radius: 10px;'
$content = $content -replace 'gap: 9px;', 'gap: 7px;'

# Update quick-icon
$content = $content -replace 'width: 36px;', 'width: 30px;'
$content = $content -replace 'height: 36px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'

# Update quick-action strong
$content = $content -replace 'font-size: .67rem;', 'font-size: .6rem;'

# Update quick-action span
$content = $content -replace 'font-size: .58rem;', 'font-size: .52rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update quick-action > i:last-child
$content = $content -replace 'font-size: .67rem;', 'font-size: .6rem;'

# Update stats-grid
$content = $content -replace 'gap: 13px;', 'gap: 10px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'

# Update stat-card
$content = $content -replace 'min-height: 95px;', 'min-height: 76px;'
$content = $content -replace 'padding: 15px;', 'padding: 11px;'
$content = $content -replace 'border-radius: 14px;', 'border-radius: 12px;'
$content = $content -replace 'gap: 11px;', 'gap: 9px;'

# Update stat-icon
$content = $content -replace 'width: 41px;', 'width: 34px;'
$content = $content -replace 'height: 41px;', 'height: 34px;'
$content = $content -replace 'border-radius: 11px;', 'border-radius: 9px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'

# Update stat-content span
$content = $content -replace 'font-size: .63rem;', 'font-size: .58rem;'

# Update stat-content strong
$content = $content -replace 'font-size: 1.3rem;', 'font-size: 1.15rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update stat-content small
$content = $content -replace 'font-size: .58rem;', 'font-size: .53rem;'
$content = $content -replace 'margin-top: 3px;', 'margin-top: 2px;'

# Update summary-columns
$content = $content -replace 'gap: 14px;', 'gap: 10px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'

# Update summary-card-large
$content = $content -replace 'padding: 17px;', 'padding: 12px;'
$content = $content -replace 'border-radius: 15px;', 'border-radius: 12px;'

# Update section-header
$content = $content -replace 'gap: 15px;', 'gap: 10px;'
$content = $content -replace 'margin-bottom: 14px;', 'margin-bottom: 10px;'

# Update section-header > div:first-child span
$content = $content -replace 'font-size: .58rem;', 'font-size: .52rem;'
$content = $content -replace 'letter-spacing: .11em;', 'letter-spacing: .08em;'

# Update section-header h2
$content = $content -replace 'font-size: .94rem;', 'font-size: .85rem;'
$content = $content -replace 'margin: 3px 0 0;', 'margin: 2px 0 1px;'

# Update view-link
$content = $content -replace 'font-size: .62rem;', 'font-size: .55rem;'
$content = $content -replace 'gap: 6px;', 'gap: 4px;'

# Update status-summary-grid
$content = $content -replace 'gap: 9px;', 'gap: 7px;'

# Update status-box
$content = $content -replace 'padding: 10px;', 'padding: 8px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 8px;'
$content = $content -replace 'gap: 8px;', 'gap: 6px;'

# Update status-box-icon
$content = $content -replace 'width: 32px;', 'width: 28px;'
$content = $content -replace 'height: 32px;', 'height: 28px;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 7px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'

# Update status-box span
$content = $content -replace 'font-size: .57rem;', 'font-size: .5rem;'

# Update status-box strong
$content = $content -replace 'font-size: .92rem;', 'font-size: .82rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update attendance-summary-grid
$content = $content -replace 'gap: 8px;', 'gap: 6px;'

# Update attendance-status-box
$content = $content -replace 'padding: 11px;', 'padding: 8px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 8px;'

# Update attendance-status-box span
$content = $content -replace 'font-size: .57rem;', 'font-size: .5rem;'

# Update attendance-status-box strong
$content = $content -replace 'font-size: .95rem;', 'font-size: .85rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update activity-columns
$content = $content -replace 'gap: 14px;', 'gap: 10px;'
$content = $content -replace 'margin-bottom: 20px;', 'margin-bottom: 14px;'

# Update activity-card
$content = $content -replace 'border-radius: 15px;', 'border-radius: 12px;'

# Update activity-header
$content = $content -replace 'gap: 15px;', 'gap: 10px;'
$content = $content -replace 'padding: 16px 17px;', 'padding: 11px 12px;'

# Update activity-header span
$content = $content -replace 'font-size: .58rem;', 'font-size: .52rem;'
$content = $content -replace 'letter-spacing: .11em;', 'letter-spacing: .08em;'

# Update activity-header h2
$content = $content -replace 'font-size: .94rem;', 'font-size: .85rem;'
$content = $content -replace 'margin: 3px 0 0;', 'margin: 2px 0 1px;'

# Update view-link (activity)
$content = $content -replace 'font-size: .62rem;', 'font-size: .55rem;'
$content = $content -replace 'gap: 6px;', 'gap: 4px;'

# Update activity-list
$content = $content -replace 'padding: 4px 17px 7px;', 'padding: 3px 12px 5px;'

# Update activity-item
$content = $content -replace 'gap: 9px;', 'gap: 7px;'
$content = $content -replace 'padding: 11px 0;', 'padding: 8px 0;'

# Update activity-icon
$content = $content -replace 'width: 35px;', 'width: 30px;'
$content = $content -replace 'height: 35px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'

# Update activity-main strong
$content = $content -replace 'font-size: .66rem;', 'font-size: .6rem;'

# Update activity-main span
$content = $content -replace 'font-size: .59rem;', 'font-size: .53rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update activity-main small
$content = $content -replace 'font-size: .55rem;', 'font-size: .5rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update status-badge, attendance-badge
$content = $content -replace 'padding: 5px 8px;', 'padding: 4px 6px;'
$content = $content -replace 'font-size: .55rem;', 'font-size: .5rem;'

# Update empty-state
$content = $content -replace 'padding: 31px 18px;', 'padding: 22px 14px;'

# Update empty-icon
$content = $content -replace 'width: 45px;', 'width: 36px;'
$content = $content -replace 'height: 45px;', 'height: 36px;'
$content = $content -replace 'border-radius: 12px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'
$content = $content -replace 'margin: 0 auto 9px;', 'margin: 0 auto 7px;'

# Update empty-state h3
$content = $content -replace 'font-size: .8rem;', 'font-size: .7rem;'

# Update empty-state p
$content = $content -replace 'font-size: .61rem;', 'font-size: .55rem;'
$content = $content -replace 'margin: 5px auto 0;', 'margin: 4px auto 0;'

# Update modules-section
$content = $content -replace 'padding: 17px;', 'padding: 12px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'
$content = $content -replace 'border-radius: 15px;', 'border-radius: 12px;'

# Update modules-grid
$content = $content -replace 'gap: 9px;', 'gap: 7px;'

# Update module-card
$content = $content -replace 'padding: 11px;', 'padding: 8px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 8px;'
$content = $content -replace 'gap: 9px;', 'gap: 7px;'

# Update module-icon
$content = $content -replace 'width: 35px;', 'width: 30px;'
$content = $content -replace 'height: 35px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'

# Update module-card strong
$content = $content -replace 'font-size: .65rem;', 'font-size: .6rem;'

# Update module-card span
$content = $content -replace 'font-size: .56rem;', 'font-size: .5rem;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update module-card > i:last-child
$content = $content -replace 'font-size: .64rem;', 'font-size: .55rem;'

# Update access-notice
$content = $content -replace 'padding: 13px 15px;', 'padding: 10px 12px;'
$content = $content -replace 'border-radius: 13px;', 'border-radius: 10px;'
$content = $content -replace 'gap: 10px;', 'gap: 8px;'

# Update notice-icon
$content = $content -replace 'width: 35px;', 'width: 30px;'
$content = $content -replace 'height: 35px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 1rem;'

# Update access-notice strong
$content = $content -replace 'font-size: .67rem;', 'font-size: .6rem;'

# Update access-notice p
$content = $content -replace 'font-size: .6rem;', 'font-size: .55rem;'
$content = $content -replace 'margin: 3px 0 0;', 'margin: 2px 0 0;'

# Update responsive
$content = $content -replace 'padding-left: 0;\s*padding-right: 0;', 'padding-left: 0; padding-right: 0;'

Set-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Staff/Dashboard.cshtml' $content