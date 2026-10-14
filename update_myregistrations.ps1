$content = Get-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Registration/MyRegistrations.cshtml' -Raw

# Update my-registrations-page padding and background
$content = $content -replace 'padding: 24px;', 'padding: 12px 18px 18px;'
$content = $content -replace 'max-width: 1250px;', ''
$content = $content -replace 'margin: 0 auto;', ''
$content = $content -replace 'box-sizing: border-box;', ''
$content = $content -replace 'background:\s*linear-gradient\(135deg,\s*#1475f5,\s*#1555ae\);', 'background: radial-gradient(circle at 85% 5%, rgba(12,144,150,.14), transparent 27%), #031716;'
$content = $content -replace 'color: #ffffff;', 'color: #FFFFFF;'

# Update page-header
$content = $content -replace 'gap: 17px;', 'gap: 14px;'
$content = $content -replace 'padding: 25px 28px;', 'padding: 14px 18px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'
$content = $content -replace 'border-radius: 19px;', 'border-radius: 13px;'

# Update header-icon
$content = $content -replace 'width: 60px;', 'width: 38px;'
$content = $content -replace 'height: 60px;', 'height: 38px;'
$content = $content -replace 'border-radius: 16px;', 'border-radius: 10px;'
$content = $content -replace 'background: rgba\(255,255,255,.14\);', 'background: rgba(255,255,255,.09);'
$content = $content -replace 'border: 1px solid rgba\(255,255,255,.2\);', 'border: 1px solid rgba(255,255,255,.13);'
$content = $content -replace 'font-size: 25px;', 'font-size: 16px;'

# Update header-kicker
$content = $content -replace 'font-size: 10px;', 'font-size: 7px;'
$content = $content -replace 'letter-spacing: 1px;', 'letter-spacing: .8px;'

# Update page-header h1
$content = $content -replace 'font-size: 28px;', 'font-size: 20px;'
$content = $content -replace 'margin: 0 0 5px;', 'margin: 3px 0 2px;'

# Update page-header p
$content = $content -replace 'font-size: 12px;', 'font-size: 8px;'
$content = $content -replace 'opacity: .9;', ''

# Update browse-button
$content = $content -replace 'min-height: 42px;', 'min-height: 30px;'
$content = $content -replace 'padding: 0 15px;', 'padding: 0 10px;'
$content = $content -replace 'border: 1px solid rgba\(255,255,255,.25\);', 'border: 1px solid rgba(183,215,229,.16);'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 7px;'
$content = $content -replace 'background: rgba\(255,255,255,.08\);', 'background: rgba(255,255,255,.06);'
$content = $content -replace 'font-size: 10px;', 'font-size: 7px;'
$content = $content -replace 'font-weight: 700;', 'font-weight: 700;'

# Update browse-button:hover
$content = $content -replace 'background: rgba\(255,255,255,.17\);', 'background: rgba(255,255,255,.10);'

# Update profile-card
$content = $content -replace 'gap: 14px;', 'gap: 10px;'
$content = $content -replace 'padding: 18px 20px;', 'padding: 12px 14px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'
$content = $content -replace 'border-radius: 16px;', 'border-radius: 12px;'
$content = $content -replace 'background: #ffffff;', 'background: white;'
$content = $content -replace 'border: 1px solid #e4ebf2;', 'border: 1px solid #e5e9ef;'
$content = $content -replace 'box-shadow: 0 6px 22px rgba\(20,50,90,.04\);', 'box-shadow: 0 4px 16px rgba(31, 43, 70, .03);'

# Update profile-avatar
$content = $content -replace 'width: 58px;', 'width: 40px;'
$content = $content -replace 'height: 58px;', 'height: 40px;'
$content = $content -replace 'border-radius: 16px;', 'border-radius: 10px;'
$content = $content -replace 'font-size: 21px;', 'font-size: 1.25rem;'

# Update profile-info > span
$content = $content -replace 'font-size: 8px;', 'font-size: .6rem;'
$content = $content -replace 'letter-spacing: .7px;', 'letter-spacing: .1em;'
$content = $content -replace 'margin-bottom: 4px;', 'margin-bottom: 2px;'

# Update profile-info h2
$content = $content -replace 'font-size: 18px;', 'font-size: .98rem;'
$content = $content -replace 'margin: 0 0 6px;', 'margin: 0 0 3px;'

# Update profile-meta
$content = $content -replace 'gap: 12px;', 'gap: 10px;'

# Update profile-meta span
$content = $content -replace 'font-size: 9px;', 'font-size: .78rem;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'

# Update profile-meta i
$content = $content -replace 'color: #1475f5;', 'color: #1f2b46;'

# Update summary-grid
$content = $content -replace 'gap: 14px;', 'gap: 8px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'

# Update summary-card
$content = $content -replace 'min-height: 98px;', 'min-height: 76px;'
$content = $content -replace 'padding: 17px;', 'padding: 11px;'
$content = $content -replace 'border-radius: 15px;', 'border-radius: 12px;'
$content = $content -replace 'gap: 10px;', 'gap: 8px;'

# Update summary-info span
$content = $content -replace 'margin-bottom: 6px;', 'margin-bottom: 3px;'
$content = $content -replace 'font-size: 9px;', 'font-size: .65rem;'

# Update summary-info strong
$content = $content -replace 'font-size: 25px;', 'font-size: 1.5rem;'
$content = $content -replace 'line-height: 1;', 'line-height: 1.1;'

# Update summary-icon
$content = $content -replace 'width: 44px;', 'width: 34px;'
$content = $content -replace 'height: 44px;', 'height: 34px;'
$content = $content -replace 'border-radius: 12px;', 'border-radius: 9px;'
$content = $content -replace 'font-size: 18px;', 'font-size: 14px;'

# Update alert-message
$content = $content -replace 'padding: 13px 15px;', 'padding: 10px 12px;'
$content = $content -replace 'margin-bottom: 15px;', 'margin-bottom: 10px;'
$content = $content -replace 'border-radius: 11px;', 'border-radius: 9px;'
$content = $content -replace 'font-size: 9px;', 'font-size: .73rem;'

# Update alert-icon
$content = $content -replace 'font-size: 16px;', 'font-size: 1rem;'

# Update alert-message strong
$content = $content -replace 'font-size: 9px;', 'font-size: .73rem;'

# Update alert-message p
$content = $content -replace 'font-size: 9px;', 'font-size: .73rem;'
$content = $content -replace 'line-height: 1.4;', 'line-height: 1.3;'

# Update section-card
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'
$content = $content -replace 'border-radius: 17px;', 'border-radius: 12px;'
$content = $content -replace 'background: #ffffff;', 'background: white;'
$content = $content -replace 'border: 1px solid #e4ebf2;', 'border: 1px solid #e5e9ef;'
$content = $content -replace 'box-shadow: 0 6px 22px rgba\(20,50,90,.04\);', 'box-shadow: 0 4px 16px rgba(31, 43, 70, .03);'

# Update section-header
$content = $content -replace 'gap: 15px;', 'gap: 10px;'
$content = $content -replace 'padding: 19px 22px;', 'padding: 12px 14px;'

# Update section-heading
$content = $content -replace 'gap: 11px;', 'gap: 8px;'

# Update section-icon
$content = $content -replace 'width: 42px;', 'width: 34px;'
$content = $content -replace 'height: 42px;', 'height: 34px;'
$content = $content -replace 'border-radius: 11px;', 'border-radius: 9px;'
$content = $content -replace 'background: #edf5ff;', 'background: #eef2f7;'
$content = $content -replace 'color: #1475f5;', 'color: #1f2b46;'
$content = $content -replace 'font-size: 17px;', 'font-size: 1.05rem;'

# Update section-header h2
$content = $content -replace 'font-size: 17px;', 'font-size: 1rem;'
$content = $content -replace 'margin: 0 0 3px;', 'margin: 0 0 2px;'
$content = $content -replace 'color: #172940;', 'color: #202b3c;'

# Update section-header p
$content = $content -replace 'font-size: 10px;', 'font-size: .78rem;'
$content = $content -replace 'color: #7c8b9c;', 'color: #717b8c;'

# Update record-count
$content = $content -replace 'min-height: 30px;', 'min-height: 26px;'
$content = $content -replace 'padding: 0 10px;', 'padding: 0 8px;'
$content = $content -replace 'border-radius: 20px;', 'border-radius: 8px;'
$content = $content -replace 'background: #edf5ff;', 'background: #edf5ef;'
$content = $content -replace 'color: #1475f5;', 'color: #52735b;'
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'

# Update registration-list
$content = $content -replace 'padding: 18px 20px 20px;', 'padding: 10px 12px 12px;'

# Update registration-card
$content = $content -replace 'gap: 13px;', 'gap: 10px;'
$content = $content -replace 'padding: 15px;', 'padding: 11px;'
$content = $content -replace 'border-radius: 13px;', 'border-radius: 10px;'

# Update program-icon
$content = $content -replace 'width: 48px;', 'width: 38px;'
$content = $content -replace 'height: 48px;', 'height: 38px;'
$content = $content -replace 'border-radius: 12px;', 'border-radius: 9px;'
$content = $content -replace 'font-size: 18px;', 'font-size: 1.15rem;'

# Update program-title-row
$content = $content -replace 'gap: 9px;', 'gap: 7px;'
$content = $content -replace 'margin-bottom: 7px;', 'margin-bottom: 5px;'

# Update program-title-row h3
$content = $content -replace 'font-size: 13px;', 'font-size: .78rem;'

# Update program-meta
$content = $content -replace 'gap: 12px;', 'gap: 10px;'
$content = $content -replace 'margin-bottom: 8px;', 'margin-bottom: 6px;'

# Update program-meta span
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'

# Update program-meta i
$content = $content -replace 'color: #1475f5;', 'color: #1f2b46;'

# Update registration-id
$content = $content -replace 'font-size: 7px;', 'font-size: .6rem;'
$content = $content -replace 'margin-top: 3px;', 'margin-top: 2px;'

# Update registration-id strong
$content = $content -replace 'font-weight: 800;', 'font-weight: 700;'

# Update status-badge
$content = $content -replace 'gap: 5px;', 'gap: 4px;'
$content = $content -replace 'padding: 6px 9px;', 'padding: 4px 7px;'
$content = $content -replace 'font-size: 7px;', 'font-size: .6rem;'

# Update attendance-button
$content = $content -replace 'min-height: 38px;', 'min-height: 32px;'
$content = $content -replace 'padding: 0 11px;', 'padding: 0 9px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'
$content = $content -replace 'gap: 6px;', 'gap: 5px;'

# Update action-note
$content = $content -replace 'padding: 8px 10px;', 'padding: 6px 8px;'
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'
$content = $content -replace 'gap: 5px;', 'gap: 4px;'
$content = $content -replace 'border-radius: 8px;', 'border-radius: 7px;'

# Update empty-state
$content = $content -replace 'padding: 58px 20px;', 'padding: 40px 16px;'

# Update empty-icon
$content = $content -replace 'width: 60px;', 'width: 40px;'
$content = $content -replace 'height: 60px;', 'height: 40px;'
$content = $content -replace 'border-radius: 50%;', 'border-radius: 10px;'
$content = $content -replace 'font-size: 24px;', 'font-size: 1.15rem;'
$content = $content -replace 'margin: 0 auto 13px;', 'margin: 0 auto 8px;'

# Update empty-state h3
$content = $content -replace 'font-size: 17px;', 'font-size: .9rem;'
$content = $content -replace 'margin: 0 0 5px;', 'margin: 0 0 4px;'

# Update empty-state p
$content = $content -replace 'font-size: 10px;', 'font-size: .71rem;'
$content = $content -replace 'max-width: 480px;', 'max-width: 310px;'
$content = $content -replace 'margin: 0 auto 17px;', 'margin: 5px auto 10px;'

# Update empty-button
$content = $content -replace 'min-height: 41px;', 'min-height: 36px;'
$content = $content -replace 'padding: 0 14px;', 'padding: 0 12px;'
$content = $content -replace 'border-radius: 9px;', 'border-radius: 10px;'
$content = $content -replace 'font-size: 10px;', 'font-size: .7rem;'
$content = $content -replace 'font-weight: 750;', 'font-weight: 700;'
$content = $content -replace 'background: #1475f5;', 'background: #1f2b46;'
$content = $content -replace 'color: #ffffff;', 'color: white;'

# Update empty-button:hover
$content = $content -replace 'background: #0d66df;', 'background: #344054;'

# Update guide-card
$content = $content -replace 'gap: 20px;', 'gap: 16px;'
$content = $content -replace 'padding: 17px 19px;', 'padding: 12px 14px;'
$content = $content -replace 'margin-bottom: 18px;', 'margin-bottom: 12px;'
$content = $content -replace 'border-radius: 14px;', 'border-radius: 10px;'

# Update guide-icon
$content = $content -replace 'width: 38px;', 'width: 32px;'
$content = $content -replace 'height: 38px;', 'height: 32px;'
$content = $content -replace 'border-radius: 10px;', 'border-radius: 8px;'
$content = $content -replace 'font-size: 16px;', 'font-size: 1rem;'

# Update guide-title strong
$content = $content -replace 'font-size: 9px;', 'font-size: .73rem;'
$content = $content -replace 'margin-bottom: 2px;', 'margin-bottom: 1px;'

# Update guide-title span
$content = $content -replace 'font-size: 7px;', 'font-size: .6rem;'

# Update guide-items
$content = $content -replace 'gap: 12px;', 'gap: 10px;'

# Update guide-item
$content = $content -replace 'margin-bottom: 5px;', 'margin-bottom: 4px;'

# Update guide-status
$content = $content -replace 'padding: 5px 8px;', 'padding: 4px 7px;'
$content = $content -replace 'font-size: 7px;', 'font-size: .6rem;'

# Update guide-item p
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'
$content = $content -replace 'line-height: 1.4;', 'line-height: 1.3;'

# Update attendance-info
$content = $content -replace 'gap: 11px;', 'gap: 8px;'
$content = $content -replace 'padding: 14px 16px;', 'padding: 10px 12px;'
$content = $content -replace 'border-radius: 12px;', 'border-radius: 10px;'
$content = $content -replace 'background: #edf6ff;', 'background: #eef2f7;'
$content = $content -replace 'border: 1px solid #d4e7ff;', 'border: 1px solid #e5e9ef;'

# Update attendance-info-icon
$content = $content -replace 'font-size: 18px;', 'font-size: 1.25rem;'

# Update attendance-info strong
$content = $content -replace 'font-size: 9px;', 'font-size: .73rem;'
$content = $content -replace 'margin-bottom: 3px;', 'margin-bottom: 2px;'

# Update attendance-info p
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'
$content = $content -replace 'color: #6e8298;', 'color: #717b8c;'

# Update view-attendance-link
$content = $content -replace 'font-size: 8px;', 'font-size: .65rem;'
$content = $content -replace 'font-weight: 800;', 'font-weight: 700;'

# Update responsive
$content = $content -replace 'padding: 14px;', 'padding: 8px 8px 18px 8px;'
$content = $content -replace 'padding: 20px;', 'padding: 14px;'
$content = $content -replace 'font-size: 22px;', 'font-size: 1.5rem;'
$content = $content -replace 'padding: 15px;', 'padding: 12px;'
$content = $content -replace 'padding: 0 14px 14px;', 'padding: 0 8px 8px;'

# Remove page-header::after gradient overlay
$content = $content -replace '\.page-header::after \{[^}]+\}', ''

Set-Content 'C:/Users/moris_690urgs/source/repos/BYPManagementSystem/BYPManagementSystem/Views/Registration/MyRegistrations.cshtml' $content