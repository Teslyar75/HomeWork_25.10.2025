// Функция для показа сообщений об ошибках валидации (аналогично авторизации)
function showValidationMessage(type, message, errors = null) {
    const validationErrorsDiv = document.getElementById('validation-errors');
    const successDiv = document.getElementById('success-message');
    const generalErrorDiv = document.getElementById('general-error');
    const errorList = document.getElementById('error-list');
    const successText = document.getElementById('success-text');
    const errorText = document.getElementById('error-text');
    
    // Скрываем все сообщения
    validationErrorsDiv.classList.add('d-none');
    successDiv.classList.add('d-none');
    generalErrorDiv.classList.add('d-none');
    
    // Очищаем все поля от ошибок
    clearFieldErrors();
    
    if (type === 'validation') {
        // Показываем ошибки валидации
        errorList.innerHTML = '';
        for (const [field, errorMessage] of Object.entries(errors)) {
            const li = document.createElement('li');
            li.textContent = `${field}: ${errorMessage}`;
            errorList.appendChild(li);
            
            // Показываем ошибку под соответствующим полем
            showFieldError(field, errorMessage);
        }
        validationErrorsDiv.classList.remove('d-none');
    } else if (type === 'success') {
        // Показываем сообщение об успехе
        successText.textContent = message;
        successDiv.classList.remove('d-none');
    } else if (type === 'error') {
        // Показываем общую ошибку
        errorText.textContent = message;
        generalErrorDiv.classList.remove('d-none');
    }
}

// Функция для показа ошибки под конкретным полем
function showFieldError(fieldName, errorMessage) {
    const fieldMap = {
        'Name': 'name',
        'Description': 'description', 
        'Slug': 'slug',
        'Image': 'image'
    };
    
    const fieldId = fieldMap[fieldName];
    if (fieldId) {
        const field = document.getElementById(`group-${fieldId}`);
        const errorDiv = document.getElementById(`${fieldId}-error`);
        
        if (field && errorDiv) {
            field.classList.add('is-invalid');
            errorDiv.textContent = errorMessage;
        }
    }
}

// Функция для очистки ошибок полей
function clearFieldErrors() {
    const fields = ['name', 'description', 'slug', 'image'];
    fields.forEach(fieldId => {
        const field = document.getElementById(`group-${fieldId}`);
        const errorDiv = document.getElementById(`${fieldId}-error`);
        
        if (field) field.classList.remove('is-invalid');
        if (errorDiv) errorDiv.textContent = '';
    });
}

// Клиентская валидация перед отправкой формы
function validateForm(formData) {
    const errors = {};
    
    // Валидация названия
    const name = formData.get('group-name')?.trim();
    if (!name) {
        errors['Name'] = 'Назва групи не може бути порожньою';
    } else if (name.length < 2) {
        errors['Name'] = 'Назва групи повинна містити мінімум 2 символи';
    } else if (name.length > 100) {
        errors['Name'] = 'Назва групи не може бути довшою за 100 символів';
    }
    
    // Валидация описания
    const description = formData.get('group-description')?.trim();
    if (description && description.length > 500) {
        errors['Description'] = 'Опис групи не може бути довшим за 500 символів';
    }
    
    // Валидация slug
    const slug = formData.get('group-slug')?.trim();
    if (!slug) {
        errors['Slug'] = 'Slug не може бути порожнім';
    } else if (slug.length < 2) {
        errors['Slug'] = 'Slug повинен містити мінімум 2 символи';
    } else if (slug.length > 50) {
        errors['Slug'] = 'Slug не може бути довшим за 50 символів';
    } else if (!/^[a-z0-9\-]+$/.test(slug)) {
        errors['Slug'] = 'Slug може містити тільки малі літери, цифри та дефіси';
    }
    
    // Валидация изображения
    const image = formData.get('group-image');
    if (!image || image.size === 0) {
        errors['Image'] = 'Необхідно вибрати зображення';
    } else if (image.size > 5 * 1024 * 1024) {
        errors['Image'] = 'Розмір зображення не може перевищувати 5MB';
    } else {
        const allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp'];
        const extension = '.' + image.name.split('.').pop().toLowerCase();
        if (!allowedExtensions.includes(extension)) {
            errors['Image'] = `Дозволені формати: ${allowedExtensions.join(', ')}`;
        }
    }
    
    return errors;
}

document.addEventListener('submit', e => {
    const form = e.target;
    if (form.id == "admin-group-form") {
        e.preventDefault();
        
        const submitButton = form.querySelector('button[type="submit"]');
        const formData = new FormData(form);
        
        // Очищаем предыдущие сообщения
        showValidationMessage('clear');
        
        // Клиентская валидация
        const clientErrors = validateForm(formData);
        if (Object.keys(clientErrors).length > 0) {
            showValidationMessage('validation', 'Помилки валідації', clientErrors);
            return;
        }
        
        // Блокируем кнопку и показываем процесс
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Створення...';
        
        fetch("/api/group", {
            method: "POST",
            body: formData
        })
        .then(r => r.json())
        .then(response => {
            // Проверяем оба варианта (Status и status) для совместимости
            const status = response.Status || response.status;
            
            if (status === "Ok") {
                // Успех - показываем сообщение и очищаем форму
                const message = response.Message || response.message || "Нова група створена";
                showValidationMessage('success', message);
                form.reset(); // Очищаем форму
            } else if (status === "ValidationError") {
                // Ошибки валидации с сервера - показываем их
                showValidationMessage('validation', 'Помилки валідації', response.Errors || response.errors);
            } else {
                // Общая ошибка - показываем детали для отладки
                const errorMsg = response.ErrorMessage || response.errorMessage || "Невідома помилка";
                const details = (response.Details || response.details) ? ` (${response.Details || response.details})` : '';
                showValidationMessage('error', errorMsg + details);
                
                // Логируем в консоль для отладки
                console.error('Server error:', response);
            }
        })
        .catch(error => {
            showValidationMessage('error', "Помилка мережі: " + error.message);
        })
        .finally(() => {
            // Разблокируем кнопку
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="bi bi-plus-circle"></i> Створити групу';
        });
    }
});
