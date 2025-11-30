// Admin Book Management Modal Functions
(function ($) {
    'use strict';

    // Load modal content
    window.loadModal = function (url, title) {
        $('#bookModalLabel').html('<i class="fas fa-book"></i> ' + title);
        $('#modalContent').html(
            '<div class="modal-body text-center py-5">' +
            '<div class="spinner-border text-primary" role="status">' +
            '<span class="visually-hidden">Loading...</span>' +
            '</div>' +
            '<p class="mt-3">Đang tải...</p>' +
            '</div>'
        );

        var myModal = new bootstrap.Modal(document.getElementById('bookModal'));
        myModal.show();

        $.ajax({
            url: url,
            type: 'GET',
            success: function (data) {
                $('#modalContent').html(data);
            },
            error: function () {
                $('#modalContent').html(
                    '<div class="modal-body">' +
                    '<div class="alert alert-danger">' +
                    '<i class="fas fa-exclamation-circle"></i> Có lỗi xảy ra khi tải dữ liệu.' +
                    '</div>' +
                    '</div>'
                );
            }
        });
    };

    // Show alert message
    window.showAlert = function (type, message) {
        var iconClass = type === 'success' ? 'check-circle' : 
                       type === 'warning' ? 'exclamation-triangle' : 
                       'exclamation-circle';
        
        var alertHtml = '<div class="alert alert-' + type + ' alert-dismissible fade show" role="alert">' +
            '<i class="fas fa-' + iconClass + '"></i> ' +
            message +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            '</div>';
        
        $('.container-fluid.px-4').prepend(alertHtml);

        // Auto dismiss after 5 seconds
        setTimeout(function () {
            $('.alert').fadeOut('slow', function () {
                $(this).remove();
            });
        }, 5000);
    };

    // Submit create form
    window.submitCreateForm = function () {
        var form = $('#createSachForm')[0];
        var formData = new FormData(form);

        $.ajax({
            url: form.action,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    var myModal = bootstrap.Modal.getInstance(document.getElementById('bookModal'));
                    myModal.hide();
                    showAlert('success', response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                } else {
                    showAlert('danger', response.message);
                }
            },
            error: function (xhr) {
                var message = 'Có lỗi xảy ra. Vui lòng thử lại.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                }
                showAlert('danger', message);
            }
        });
    };

    // Submit edit form
    window.submitEditForm = function () {
        var form = $('#editSachForm')[0];
        var formData = new FormData(form);

        $.ajax({
            url: form.action,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    var myModal = bootstrap.Modal.getInstance(document.getElementById('bookModal'));
                    myModal.hide();
                    showAlert('success', response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                } else {
                    showAlert('danger', response.message);
                }
            },
            error: function (xhr) {
                var message = 'Có lỗi xảy ra. Vui lòng thử lại.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                }
                showAlert('danger', message);
            }
        });
    };

    // Edit book from details modal
    window.editBook = function (id, baseUrl) {
        var myModal = bootstrap.Modal.getInstance(document.getElementById('bookModal'));
        myModal.hide();
        
        setTimeout(function () {
            loadModal(baseUrl + '?id=' + id, 'Chỉnh sửa sách');
        }, 300);
    };

    // Initialize modal events
    $(document).ready(function () {
        $('#bookModal').on('hidden.bs.modal', function () {
            $('#modalContent').html(
                '<div class="modal-body text-center py-5">' +
                '<div class="spinner-border text-primary" role="status">' +
                '<span class="visually-hidden">Loading...</span>' +
                '</div>' +
                '<p class="mt-3">Đang tải...</p>' +
                '</div>'
            );
        });

        // Handle form submission with Enter key
        $(document).on('keypress', 'form', function (e) {
            if (e.which === 13 && e.target.tagName !== 'TEXTAREA') {
                e.preventDefault();
                return false;
            }
        });
    });

})(jQuery);
