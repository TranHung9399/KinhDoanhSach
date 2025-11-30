/**
 * Admin Common JavaScript
 * Shared functionality for all admin pages
 */

(function ($) {
    'use strict';

    // Initialize on document ready
    $(document).ready(function () {
        initializeDataTables();
        initializeTooltips();
        initializeConfirmDialogs();
        initializeAlerts();
        initializeAjaxForms();
        initializeFileUpload();
    });

    /**
     * Initialize DataTables
     */
    function initializeDataTables() {
        if ($.fn.DataTable) {
            $('.table').DataTable({
                language: {
                    url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/vi.json'
                },
                pageLength: 10,
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "Tất cả"]],
                dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                     '<"row"<"col-sm-12"tr>>' +
                     '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
                responsive: true,
                order: [[0, 'desc']]
            });
        }
    }

    /**
     * Initialize Bootstrap tooltips
     */
    function initializeTooltips() {
        if ($.fn.tooltip) {
            $('[data-bs-toggle="tooltip"]').tooltip();
            $('[title]').tooltip();
        }
    }

    /**
     * Initialize confirm dialogs for delete actions
     */
    function initializeConfirmDialogs() {
        $(document).on('click', '[data-confirm]', function (e) {
            const message = $(this).data('confirm') || 'Bạn có chắc chắn muốn thực hiện hành động này?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        });

        // Delete buttons
        $(document).on('click', '.btn-delete, a[href*="Delete"]', function (e) {
            if (!$(this).data('confirm-handled')) {
                const itemName = $(this).closest('tr').find('td:eq(1)').text().trim();
                const message = `Bạn có chắc muốn xóa "${itemName}"?\nHành động này không thể hoàn tác!`;
                
                if (!confirm(message)) {
                    e.preventDefault();
                    return false;
                }
                $(this).data('confirm-handled', true);
            }
        });
    }

    /**
     * Auto-dismiss alerts after 5 seconds
     */
    function initializeAlerts() {
        $('.alert:not(.alert-permanent)').each(function () {
            const $alert = $(this);
            setTimeout(function () {
                $alert.fadeOut(300, function () {
                    $(this).remove();
                });
            }, 5000);
        });
    }

    /**
     * AJAX form submission with loading indicator
     */
    function initializeAjaxForms() {
        $(document).on('submit', '.ajax-form', function (e) {
            e.preventDefault();
            
            const $form = $(this);
            const $submitBtn = $form.find('button[type="submit"]');
            const originalText = $submitBtn.html();

            // Show loading
            $submitBtn.prop('disabled', true)
                      .html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');

            $.ajax({
                url: $form.attr('action'),
                type: $form.attr('method') || 'POST',
                data: $form.serialize(),
                success: function (response) {
                    if (response.success) {
                        showNotification(response.message || 'Thao tác thành công!', 'success');
                        if (response.redirect) {
                            setTimeout(function () {
                                window.location.href = response.redirect;
                            }, 1000);
                        }
                    } else {
                        showNotification(response.message || 'Có lỗi xảy ra!', 'error');
                    }
                },
                error: function (xhr) {
                    showNotification('Có lỗi xảy ra khi xử lý yêu cầu!', 'error');
                    console.error(xhr);
                },
                complete: function () {
                    $submitBtn.prop('disabled', false).html(originalText);
                }
            });
        });
    }

    /**
     * File upload preview
     */
    function initializeFileUpload() {
        $(document).on('change', 'input[type="file"].image-upload', function () {
            const file = this.files[0];
            if (file) {
                const reader = new FileReader();
                const $preview = $(this).closest('.form-group').find('.image-preview');
                
                reader.onload = function (e) {
                    if ($preview.length) {
                        $preview.html(`<img src="${e.target.result}" class="img-thumbnail" style="max-width: 200px; max-height: 200px;" />`);
                    } else {
                        $(this).after(`<div class="image-preview mt-2"><img src="${e.target.result}" class="img-thumbnail" style="max-width: 200px; max-height: 200px;" /></div>`);
                    }
                }.bind(this);
                
                reader.readAsDataURL(file);
            }
        });
    }

    /**
     * Show notification toast
     */
    function showNotification(message, type) {
        const iconMap = {
            success: 'fa-check-circle',
            error: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        };

        const colorMap = {
            success: 'success',
            error: 'danger',
            warning: 'warning',
            info: 'info'
        };

        const icon = iconMap[type] || 'fa-info-circle';
        const color = colorMap[type] || 'info';

        const toast = `
            <div class="toast-notification" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px; animation: slideInRight 0.3s ease;">
                <div class="alert alert-${color} alert-dismissible fade show shadow-lg" role="alert">
                    <i class="fas ${icon} me-2"></i>
                    ${message}
                    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                </div>
            </div>
        `;

        const $toast = $(toast);
        $('body').append($toast);

        setTimeout(function () {
            $toast.fadeOut(300, function () {
                $(this).remove();
            });
        }, 3000);
    }

    /**
     * Export table to Excel
     */
    window.exportToExcel = function (tableId, filename) {
        const table = document.getElementById(tableId);
        if (!table) return;

        const wb = XLSX.utils.table_to_book(table, { sheet: "Sheet1" });
        XLSX.writeFile(wb, `${filename}_${new Date().toISOString().slice(0, 10)}.xlsx`);
    };

    /**
     * Print table
     */
    window.printTable = function () {
        window.print();
    };

    /**
     * Format currency
     */
    window.formatCurrency = function (amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount).replace('₫', 'đ');
    };

    /**
     * Debounce function for search
     */
    window.debounce = function (func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    };

    /**
     * Live search functionality
     */
    $('.search-box input').on('keyup', debounce(function () {
        const searchTerm = $(this).val().toLowerCase();
        const $table = $('.table tbody');

        $table.find('tr').each(function () {
            const text = $(this).text().toLowerCase();
            $(this).toggle(text.indexOf(searchTerm) > -1);
        });
    }, 300));

    /**
     * Toggle table row selection
     */
    $(document).on('change', '.select-all', function () {
        const isChecked = $(this).prop('checked');
        $('.select-row').prop('checked', isChecked);
        updateBulkActions();
    });

    $(document).on('change', '.select-row', function () {
        updateBulkActions();
    });

    function updateBulkActions() {
        const selectedCount = $('.select-row:checked').length;
        const $bulkActions = $('.bulk-actions');

        if (selectedCount > 0) {
            $bulkActions.show().find('.selected-count').text(selectedCount);
        } else {
            $bulkActions.hide();
        }
    }

    /**
     * Status update via AJAX
     */
    window.updateStatus = function (id, status, type) {
        if (!confirm('Bạn có chắc muốn cập nhật trạng thái?')) {
            return;
        }

        $.ajax({
            url: `/Admin/${type}/UpdateStatus`,
            type: 'POST',
            data: { id: id, status: status },
            success: function (response) {
                if (response.success) {
                    showNotification('Cập nhật trạng thái thành công!', 'success');
                    location.reload();
                } else {
                    showNotification(response.message || 'Cập nhật thất bại!', 'error');
                }
            },
            error: function () {
                showNotification('Có lỗi xảy ra!', 'error');
            }
        });
    };

    /**
     * Copy to clipboard
     */
    window.copyToClipboard = function (text) {
        const $temp = $('<textarea>');
        $('body').append($temp);
        $temp.val(text).select();
        document.execCommand('copy');
        $temp.remove();
        showNotification('Đã sao chép vào clipboard!', 'success');
    };

    // Expose functions globally
    window.AdminCommon = {
        showNotification: showNotification,
        formatCurrency: formatCurrency,
        exportToExcel: exportToExcel,
        printTable: printTable,
        copyToClipboard: copyToClipboard,
        updateStatus: updateStatus
    };

})(jQuery);
