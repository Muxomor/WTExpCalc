window.techTreeFunctions = {
    positionFolderPopup: function (popupId, targetNodeId, containerId) {
        requestAnimationFrame(() => { // Даем DOM время обновиться
            const popupElement = document.getElementById(popupId);
            const targetNodeElement = document.getElementById(targetNodeId); // Заголовок папки
            const containerElement = document.getElementById(containerId); // Контейнер дерева

            if (!popupElement || !targetNodeElement || !containerElement) {
                console.error("Popup positioning error: Elements not found.",
                    { popupId, targetNodeId, containerId, popupElement, targetNodeElement, containerElement });
                if (popupElement) popupElement.style.visibility = 'hidden';
                return;
            }

            const targetRect = targetNodeElement.getBoundingClientRect();
            const containerRect = containerElement.getBoundingClientRect();

            // Вычисляем позицию относительно контейнера
            // Попап появится под заголовком папки, выровненный по левому краю
            const top = targetRect.bottom - containerRect.top + (containerElement.scrollTop || 0) + 5; // +5px отступ снизу
            const left = targetRect.left - containerRect.left + (containerElement.scrollLeft || 0);

            // Устанавливаем стили и делаем видимым
            popupElement.style.left = `${left}px`;
            popupElement.style.top = `${top}px`;
            popupElement.style.visibility = 'visible';
            console.log(`Positioned popup ${popupId} at top: ${top}, left: ${left}`);
        });
    },
    copyTextToClipboard: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            console.log('Async: Copying to clipboard was successful!');
            // Здесь можно было бы показать короткое уведомление пользователю
        }, function (err) {
            console.error('Async: Could not copy text: ', err);
            // Можно показать сообщение об ошибке
            alert("Не удалось скопировать текст."); // Простое уведомление
        });
    }, copyTextToClipboard_fallback: function (text) {
        try {
            // Создаем временный элемент textarea
            const textArea = document.createElement("textarea");

            // Скрываем его от пользователя
            textArea.style.position = 'fixed';
            textArea.style.top = '-9999px';
            textArea.style.left = '-9999px';
            textArea.style.opacity = '0';

            // Задаем текст и добавляем в DOM
            textArea.value = text;
            document.body.appendChild(textArea);

            // Фокусируемся и выделяем текст
            textArea.focus();
            textArea.select();

            // Пытаемся скопировать
            const successful = document.execCommand('copy');

            // Удаляем временный элемент
            document.body.removeChild(textArea);

            if (successful) {
                console.log('Fallback: Copying to clipboard was successful!');
                // Можно добавить простое уведомление
                // alert("Текст скопирован!");
            } else {
                console.error('Fallback: document.execCommand("copy") failed.');
                alert("Не удалось скопировать текст (fallback). Браузер не поддерживает или заблокировал команду.");
            }
        } catch (err) {
            console.error('Fallback: Exception while copying text: ', err);
            alert("Ошибка при копировании текста (fallback).");
        }
    },
    drawConnections: function (connectionData) {
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                console.log("Drawing connections for:", connectionData);
                if (!connectionData || connectionData.length === 0) {
                    console.log("No connection data provided.");
                    return;
                }

                const svg = document.querySelector('.tree-connections-overlay');
                if (!svg) {
                    console.error("SVG overlay not found!");
                    return;
                }
                const svgRect = svg.getBoundingClientRect();

                connectionData.forEach(conn => {
                    const line = svg.querySelector(`#${conn.lineId}`);
                    // Используем ID внутреннего элемента node-<id> для координат
                    const sourceElem = document.getElementById(conn.sourceElementId);
                    const targetElem = document.getElementById(conn.targetElementId);

                    // Проверяем видимость элементов (важно!)
                    const isSourceVisible = sourceElem && sourceElem.offsetParent !== null;
                    const isTargetVisible = targetElem && targetElem.offsetParent !== null;


                    if (line && sourceElem && targetElem && isSourceVisible && isTargetVisible) {
                        try {
                            const sourceRect = sourceElem.getBoundingClientRect();
                            const targetRect = targetElem.getBoundingClientRect();

                            // ID родительских контейнеров (для проверки на вложенность в одну папку)
                            // Используем closest('.tree-grid-item') для надежности
                            const sourceParentContainer = sourceElem.closest('.tree-grid-item');
                            const targetParentContainer = targetElem.closest('.tree-grid-item');
                            const sourceInFolder = sourceElem.closest('.folder-items-container');
                            const targetInFolder = targetElem.closest('.folder-items-container');


                            let x1, y1, x2, y2; // Объявляем переменные здесь

                            // Логика для соединений ВНУТРИ ОДНОЙ папки (оставляем как есть - вертикально)
                            if (sourceInFolder && targetInFolder && sourceParentContainer === targetParentContainer) {
                                x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left; // Середина по X
                                y1 = sourceRect.bottom - svgRect.top;                      // Низ
                                x2 = targetRect.left + targetRect.width / 2 - svgRect.left; // Середина по X
                                y2 = targetRect.top - svgRect.top;                          // Верх
                                // console.log(`Vertical connection inside folder: ${conn.sourceElementId} -> ${conn.targetElementId}`);
                            }
                            // --- ИЗМЕНЕНИЯ ЗДЕСЬ: Логика для ОСНОВНЫХ соединений (между ячейками grid) ---
                            else {
                                x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left; // ИСХОДНЫЙ: Середина по X
                                y1 = sourceRect.bottom - svgRect.top;                      // ИСХОДНЫЙ: Низ
                                x2 = targetRect.left + targetRect.width / 2 - svgRect.left; // ЦЕЛЕВОЙ: Середина по X
                                y2 = targetRect.top - svgRect.top;                          // ЦЕЛЕВОЙ: Верх
                                // console.log(`Center-Bottom to Center-Top connection: ${conn.sourceElementId} -> ${conn.targetElementId}`);
                            }
                            // --- КОНЕЦ ИЗМЕНЕНИЙ ---


                            line.setAttribute('x1', x1);
                            line.setAttribute('y1', y1);
                            line.setAttribute('x2', x2);
                            line.setAttribute('y2', y2);
                            line.style.visibility = 'visible';

                        } catch (e) {
                            console.error("Error calculating line coordinates for conn:", conn, e);
                            if (line) line.style.visibility = 'hidden';
                        }
                    } else {
                        // Скрываем линию, если один из элементов не найден или не видим
                        if (line) line.style.visibility = 'hidden';
                        /*
                        if (!sourceElem) console.warn(`Source element not found: ${conn.sourceElementId}`);
                        if (!targetElem) console.warn(`Target element not found: ${conn.targetElementId}`);
                        if (sourceElem && !isSourceVisible) console.warn(`Source element not visible: ${conn.sourceElementId}`);
                        if (targetElem && !isTargetVisible) console.warn(`Target element not visible: ${conn.targetElementId}`);
                        */
                    }
                });
            });
        });
    }
};