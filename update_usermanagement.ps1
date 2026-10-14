$content = Get-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/UserManagement/Index.cshtml' -Raw

# Update user-management-page padding
$content = $content -replace 'padding: 18px 26px 24px;', 'padding: 12px 18px 18px;'

# Update page-header
$content = $content -replace 'margin-bottom: 14px;', 'margin-bottom: 12px;'
$content = $content -replace 'gap: 20px;', 'gap: 14px;'

# Update page-eyebrow
$content = $content -replace 'font-size: 9px;', 'font-size: 7px;'
$content = $content -replace 'letter-spacing: 1.5px;', 'letter-spacing: 1px;'

# Update page-header h1
$content = $content -replace 'font-size: 26px;', 'font-size: 20px;'
$content = $content -replace 'margin: 6px 0 5px;', 'margin: 3px 0 2px;'

# Update page-header p
$content = $content -replace 'font-size: 10px;', 'font-size: 8px;'

# Update header-actions
$content = $content -replace 'gap: 9px;', 'gap: 7px;'

# Update header-button
$content = $content -replace 'min-height: 42px;', 'min-height: 30px;'
$content = $content -replace 'padding: 0 14px;', 'padding: 0 10px;'
$content = $content -replace 'font-size: .74rem;', 'font-size: 7px;'
$content = $content -replace 'border-radius: 11px;', 'border-radius: 7px;'
$content = $content -replace 'gap: 8px;', 'gap: 5px;'

# Update summary-grid
$content = $content -replace 'margin-bottom: 12px;', 'margin-bottom: 8px;'
$content = $content -replace 'gap: 10px;', 'gap: 8px;'
$content = $content -replace 'repeat\(6, minmax\(0, 1fr\)\)', 'repeat(4, minmax(0, 1fr))'

# Update summary-card
$content = $content -replace 'padding: 11px 14px;', 'padding: 10px 11px;'
$content = $content -replace 'border-radius: 14px;', 'border-radius: 10px;'
$content = $content -replace 'box-shadow: 0 5px 20px rgba\(31, 43, 70, .035\);', 'box-shadow: 0 4px 16px rgba(31, 43, 70, .03);'

# Update summary-icon
$content = $content -replace 'width: 37px;', 'width: 34px;'
$content = $content -replace 'height: 37px;', 'height: 34px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 9px;'
$content = $content -replace 'font-size: .95rem;', 'font-size: 14px;'

# Update summary-content
$content = $content -replace 'font-size: .62rem;', 'font-size: 6px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 16px;'
$content = $content -replace 'margin-top: 1px;', 'margin-top: 1px;'
$content = $content -replace 'font-size: .57rem;', 'font-size: 6px;'
$content = $content -replace 'margin-top: 3px;', 'margin-top: 2px;'

# Update filter-card, users-card, admin-note
$content = $content -replace 'border-radius: 16px;', 'border-radius: 11px;'
$content = $content -replace 'box-shadow: 0 6px 24px rgba\(31, 43, 70, .035\);', 'box-shadow: 0 4px 16px rgba(31, 43, 70, .03);'

# Update filter-card padding
$content = $content -replace 'padding: 14px;', 'padding: 10px;'
$content = $content -replace 'margin-bottom: 12px;', 'margin-bottom: 8px;'

# Update filter-header
$content = $content -replace 'gap: 20px;', 'gap: 12px;'
$content = $content -replace 'margin-bottom: 14px;', 'margin-bottom: 8px;'

# Update filter-header > div span
$content = $content -replace 'font-size: .61rem;', 'font-size: 6px;'
$content = $content -replace 'letter-spacing: .11em;', 'letter-spacing: .08em;'

# Update filter-header h2
$content = $content -replace 'font-size: .93rem;', 'font-size: 12px;'
$content = $content -replace 'margin: 3px 0 0;', 'margin: 2px 0 1px;'

# Update reset-link
$content = $content -replace 'font-size: .68rem;', 'font-size: 7px;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'

# Update filter-form
$content = $content -replace 'gap: 11px;', 'gap: 8px;'

# Update filter-field label
$content = $content -replace 'font-size: .64rem;', 'font-size: 6px;'

# Update filter-field input, select
$content = $content -replace 'height: 40px;', 'height: 30px;'
$content = $content -replace 'padding: 0 11px;', 'padding: 0 8px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 6px;'
$content = $content -replace 'font-size: .7rem;', 'font-size: 7px;'

# Update search-field input
$content = $content -replace 'padding-left: 31px;', 'padding-left: 26px;'

# Update search-button
$content = $content -replace 'height: 40px;', 'height: 30px;'
$content = $content -replace 'padding: 0 14px;', 'padding: 0 10px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 6px;'
$content = $content -replace 'font-size: .69rem;', 'font-size: 7px;'
$content = $content -replace 'gap: 7px;', 'gap: 5px;'

# Update users-card-header
$content = $content -replace 'gap: 20px;', 'gap: 12px;'
$content = $content -replace 'padding: 14px 16px;', 'padding: 10px 12px;'

# Update users-card-header > div:first-child span
$content = $content -replace 'font-size: .6rem;', 'font-size: 6px;'
$content = $content -replace 'letter-spacing: .11em;', 'letter-spacing: .08em;'

# Update users-card-header h2
$content = $content -replace 'font-size: .96rem;', 'font-size: 12px;'
$content = $content -replace 'margin: 3px 0 0;', 'margin: 2px 0 1px;'

# Update record-count
$content = $content -replace 'padding: 6px 9px;', 'padding: 4px 7px;'
$content = $content -replace 'border-radius: 999px;', 'border-radius: 5px;'
$content = $content -replace 'font-size: .61rem;', 'font-size: 6px;'

# Update users-table th
$content = $content -replace 'padding: 11px 15px;', 'padding: 8px 12px;'
$content = $content -replace 'font-size: .58rem;', 'font-size: 6px;'

# Update users-table td
$content = $content -replace 'padding: 13px 15px;', 'padding: 9px 12px;'
$content = $content -replace 'font-size: .69rem;', 'font-size: 7px;'

# Update user-avatar
$content = $content -replace 'width: 37px;', 'width: 30px;'
$content = $content -replace 'height: 37px;', 'height: 30px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 7px;'
$content = $content -replace 'font-size: 1.25rem;', 'font-size: 12px;'

# Update user-info strong
$content = $content -replace 'font-size: .72rem;', 'font-size: 8px;'
$content = $content -replace 'max-width: 190px;', 'max-width: 160px;'

# Update user-info span
$content = $content -replace 'font-size: .57rem;', 'font-size: 6px;'
$content = $content -replace 'max-width: 190px;', 'max-width: 160px;'
$content = $content -replace 'margin-top: 2px;', 'margin-top: 1px;'

# Update role-badge
$content = $content -replace 'padding: 5px 9px;', 'padding: 4px 7px;'
$content = $content -replace 'font-size: .59rem;', 'font-size: 6px;'

# Update status-badge
$content = $content -replace 'padding: 5px 9px;', 'padding: 4px 7px;'
$content = $content -replace 'font-size: .59rem;', 'font-size: 6px;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'

# Update email-cell
$content = $content -replace 'gap: 6px;', 'gap: 5px;'
$content = $content -replace 'font-size: 9px;', 'font-size: 7px;'

# Update confirmed-text, unconfirmed-text
$content = $content -replace 'font-size: .55rem;', 'font-size: 6px;'
$content = $content -replace 'margin-top: 4px;', 'margin-top: 2px;'

# Update action-button
$content = $content -replace 'min-height: 31px;', 'min-height: 26px;'
$content = $content -replace 'padding: 0 8px;', 'padding: 0 6px;'
$content = $content -replace 'font-size: .58rem;', 'font-size: 6px;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 6px;'

# Update self-badge
$content = $content -replace 'min-height: 31px;', 'min-height: 26px;'
$content = $content -replace 'padding: 0 8px;', 'padding: 0 6px;'
$content = $content -replace 'font-size: .56rem;', 'font-size: 6px;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 6px;'

# Update empty-state
$content = $content -replace 'padding: 52px 20px;', 'padding: 36px 16px;'

# Update empty-icon
$content = $content -replace 'width: 52px;', 'width: 36px;'
$content = $content -replace 'height: 52px;', 'height: 36px;'
$content = $content -replace 'border-radius: 14px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 14px;'
$content = $content -replace 'margin: 0 auto 11px;', 'margin: 0 auto 8px;'

# Update empty-state h3
$content = $content -replace 'font-size: .95rem;', 'font-size: 10px;'

# Update empty-state p
$content = $content -replace 'font-size: .69rem;', 'font-size: 7px;'
$content = $content -replace 'margin: 6px auto 13px;', 'margin: 5px auto 10px;'
$content = $content -replace 'max-width: 360px;', 'max-width: 280px;'

# Update empty-button
$content = $content -replace 'min-height: 36px;', 'min-height: 26px;'
$content = $content -replace 'padding: 0 12px;', 'padding: 0 9px;'
$content = $content -replace 'font-size: .66rem;', 'font-size: 6px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 6px;'
$content = $content -replace 'gap: 7px;', 'gap: 4px;'

# Update admin-note
$content = $content -replace 'padding: 14px 16px;', 'padding: 10px 12px;'
$content = $content -replace 'gap: 11px;', 'gap: 8px;'

# Update admin-note-icon
$content = $content -replace 'width: 35px;', 'width: 30px;'
$content = $content -replace 'height: 35px;', 'height: 30px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 12px;'

# Update admin-note strong
$content = $content -replace 'font-size: .7rem;', 'font-size: 8px;'

# Update admin-note p
$content = $content -replace 'font-size: .64rem;', 'font-size: 7px;'
$content = $content -replace 'margin: 3px 0 0;', 'margin: 2px 0 0;'

# Update delete-modal-content
$content = $content -replace 'padding: 6px;', 'padding: 4px;'
$content = $content -replace 'border-radius: 16px;', 'border-radius: 12px;'

# Update delete-modal-icon
$content = $content -replace 'width: 46px;', 'width: 36px;'
$content = $content -replace 'height: 46px;', 'height: 36px;'
$content = $content -replace 'border-radius: 13px;', 'border-radius: 10px;'
$content = $content -replace 'margin: 16px auto 0;', 'margin: 12px auto 0;'
$content = $content -replace 'font-size: 1.15rem;', 'font-size: 14px;'

# Update delete-modal-body
$content = $content -replace 'padding: 12px 22px 4px;', 'padding: 8px 16px 3px;'

# Update delete-modal-body h3
$content = $content -replace 'font-size: 1.02rem;', 'font-size: 10px;'
$content = $content -replace 'margin: 0 0 6px;', 'margin: 0 0 4px;'

# Update delete-modal-body p
$content = $content -replace 'font-size: .74rem;', 'font-size: 7px;'

# Update delete-modal-warning
$content = $content -replace 'padding: 7px 11px;', 'padding: 5px 8px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'
$content = $content -replace 'font-size: .66rem;', 'font-size: 6px;'
$content = $content -replace 'gap: 6px;', 'gap: 5px;'

# Update delete-modal-footer
$content = $content -replace 'padding: 12px 22px 18px;', 'padding: 8px 16px 12px;'

# Update delete-modal-cancel, delete-modal-confirm
$content = $content -replace 'min-height: 38px;', 'min-height: 30px;'
$content = $content -replace 'padding: 0 14px;', 'padding: 0 10px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 7px;'
$content = $content -replace 'font-size: .7rem;', 'font-size: 7px;'
$content = $content -replace 'gap: 6px;', 'gap: 5px;'

# Update responsive
$content = $content -replace 'padding-left: 0;\s*padding-right: 0;', 'padding-left: 0; padding-right: 0;'

Set-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/UserManagement/Index.cshtml' $content