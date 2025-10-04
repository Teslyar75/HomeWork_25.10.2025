class Base64 {
    static #textEncoder = new TextEncoder();
    static #textDecoder = new TextDecoder();

    // https://datatracker.ietf.org/doc/html/rfc4648#section-4
    static encode = (str) => btoa(String.fromCharCode(...Base64.#textEncoder.encode(str)));
    static decode = (str) => Base64.#textDecoder.decode(Uint8Array.from(atob(str), c => c.charCodeAt(0)));

    // https://datatracker.ietf.org/doc/html/rfc4648#section-5
    static encodeUrl = (str) => this.encode(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    static decodeUrl = (str) => this.decode(str.replace(/\-/g, '+').replace(/\_/g, '/'));

    static jwtEncodeBody = (header, payload) => this.encodeUrl(JSON.stringify(header)) + '.' + this.encodeUrl(JSON.stringify(payload));
    static jwtDecodePayload = (jwt) => JSON.parse(this.decodeUrl(jwt.split('.')[1]));
}

// Функция для показа сообщений в модальном окне авторизации
function showAuthMessage(type, message) {
    const errorDiv = document.getElementById('auth-error');
    const successDiv = document.getElementById('auth-success');
    const errorText = document.getElementById('auth-error-text');
    const successText = document.getElementById('auth-success-text');
    
    // Скрываем все сообщения
    errorDiv.classList.add('d-none');
    successDiv.classList.add('d-none');
    
    if (type === 'error') {
        errorText.textContent = message;
        errorDiv.classList.remove('d-none');
    } else if (type === 'success') {
        successText.textContent = message;
        successDiv.classList.remove('d-none');
    }
}

document.addEventListener('submit', e => {
    const form = e.target;
    if (form.id == "auth-form") {
        e.preventDefault();
        const data = new FormData(form);
        const login = data.get("auth-login");
        const password = data.get("auth-password");
        // RFC 7617
        const userPass = login + ':' + password;
        const credentials = Base64.encode(userPass);
        fetch("/User/SignIn", {
            headers: {
                "Authorization": "Basic " + credentials
            }
        }).then(r => r.json()).then(j => {
            if (j.status == 200) {
                // Показываем успешное сообщение
                showAuthMessage('success', 'Привіт: ' + j.userName);
                
                // Через 1.5 секунды перезагружаем страницу
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            }
            else {
                // Показываем ошибку
                showAuthMessage('error', j.data || 'Помилка автентифікації');
            }
        }).catch(error => {
            showAuthMessage('error', 'Помилка мережі: ' + error.message);
        });

        console.log(login, password, credentials);
    }
});

document.addEventListener('DOMContentLoaded', e => {
    let btn = document.getElementById('btn-profile-edit');
    if (btn) btn.onclick = btnProfileEditClick;
    btn = document.getElementById('btn-profile-delete');
    if (btn) btn.onclick = btnProfileDeleteClick;
});

function btnProfileEditClick() {
    const elements = document.querySelectorAll("[data-editable]");
    const editButton = document.getElementById('btn-profile-edit');
    if (elements.length == 0) return;

    if (elements[0].hasAttribute("contenteditable")) {
        // Режим сохранения - выходим из редактирования
        let wasChanges = false;
        let changes = {};
        for (let elem of elements) {
            elem.removeAttribute("contenteditable");
            if (elem.originText != elem.innerText) {
                changes[elem.getAttribute("data-editable")] = elem.innerText;
                wasChanges = true;
            }
        }
        
        // Возвращаем кнопку в исходное состояние
        editButton.textContent = "Edit";
        editButton.className = "btn btn-warning";
        
        if (wasChanges) {
            // Создаем детальное сообщение о изменениях
            let changesMessage = "Ви внесли наступні зміни:\n\n";
            for (let [field, value] of Object.entries(changes)) {
                changesMessage += `• ${field}: "${value}"\n`;
            }
            changesMessage += "\nПідтверджуєте збереження цих змін?";
            
            if (confirm(changesMessage)) {
                console.log(changes);
                
                // Блокируем кнопку во время сохранения
                editButton.disabled = true;
                editButton.textContent = "Збереження...";
                
                fetch("/User/Update", {
                    method: "PATCH",
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(changes)
                }).then(r => r.json()).then(j => {
                    if (j.status == 200) {
                        alert("✅ Зміни успішно збережено!\n\nСторінка буде оновлена через секунду.");
                        // Перезагружаем страницу для обновления данных
                        setTimeout(() => {
                            window.location.reload();
                        }, 1000);
                    }
                    else {
                        alert("❌ Помилка збереження:\n\n" + j.data + "\n\nЗміни скасовано.");
                        // Возвращаем исходные значения при ошибке
                        for (let elem of elements) {
                            elem.innerText = elem.originText;
                        }
                        // Разблокируем кнопку
                        editButton.disabled = false;
                        editButton.textContent = "Edit";
                    }
                }).catch(error => {
                    alert("❌ Помилка мережі:\n\n" + error.message + "\n\nЗміни скасовано.");
                    // Возвращаем исходные значения при ошибке сети
                    for (let elem of elements) {
                        elem.innerText = elem.originText;
                    }
                    // Разблокируем кнопку
                    editButton.disabled = false;
                    editButton.textContent = "Edit";
                });
            }
            else
            {
                for (let elem of elements) {
                    elem.innerText = elem.originText;
                }
            }
        }
    }
    else
    {
        // Режим редактирования - входим в редактирование
        for (let elem of elements) {
            elem.setAttribute("contenteditable", true);
            //зберігаємо значення, що було перед редагуванням
            elem.originText = elem.innerText;
        }
        
        // Меняем кнопку на Save
        editButton.textContent = "Save";
        editButton.className = "btn btn-success";
    }
}

function btnProfileDeleteClick() {
    // Создаем более детальный диалог подтверждения
    const confirmMessage = `⚠️ УВАГА! ⚠️

Ви збираєтесь видалити свій профіль. Ця дія призведе до:

• Видалення всіх персональних даних
• Неможливості відновлення профілю
• Втрати доступу до всіх функцій сайту

Ви дійсно хочете продовжити?`;

    if (confirm(confirmMessage)) {
        // Двойное подтверждение для критических действий
        const secondConfirm = confirm("ОСТАННЄ ПІДТВЕРДЖЕННЯ!\n\nЦя дія НЕЗВОРОТНА!\n\nНатисніть OK тільки якщо ви абсолютно впевнені.");
        
        if (secondConfirm) {
            const deleteButton = document.getElementById('btn-profile-delete');
            deleteButton.disabled = true;
            deleteButton.textContent = "Видалення...";
            
            fetch("/User/Delete", {
                method: "DELETE",
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json()).then(j => {
                if (j.status == 200) {
                    alert("✅ Профіль успішно видалено.\n\nВи будете перенаправлені на головну сторінку.");
                    window.location.href = "/";
                }
                else {
                    alert("❌ Помилка видалення: " + j.data);
                    deleteButton.disabled = false;
                    deleteButton.textContent = "Delete";
                }
            }).catch(error => {
                alert("❌ Помилка мережі: " + error.message);
                deleteButton.disabled = false;
                deleteButton.textContent = "Delete";
            });
        }
    }
}