// В файле techTreeFunction.js
window.techTreeFunctions = {
    drawConnections: function (connectionData) {
        // Небольшая задержка остается полезной
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
                const svgRect = svg.getBoundingClientRect(); // Получаем один раз

                connectionData.forEach(conn => {
                    const line = svg.querySelector(`#${conn.lineId}`);
                    // Ищем элементы по ID, переданным из C#
                    const sourceElem = document.getElementById(conn.sourceElementId);
                    const targetElem = document.getElementById(conn.targetElementId);

                    // Проверяем видимость элементов (могут быть в свернутой папке)
                    const isSourceVisible = sourceElem && sourceElem.offsetParent !== null;
                    const isTargetVisible = targetElem && targetElem.offsetParent !== null;


                    if (line && sourceElem && targetElem && isSourceVisible && isTargetVisible) { // Добавили проверку видимости
                        try {
                            const sourceRect = sourceElem.getBoundingClientRect();
                            const targetRect = targetElem.getBoundingClientRect();

                            // Рассчитываем координаты относительно SVG
                            // Логика расчета точек может потребовать настройки
                            // Пример: правый центр source -> левый центр target
                            let x1 = sourceRect.right - svgRect.left;
                            let y1 = sourceRect.top + sourceRect.height / 2 - svgRect.top;
                            let x2 = targetRect.left - svgRect.left;
                            let y2 = targetRect.top + targetRect.height / 2 - svgRect.top;

                            // ---- Дополнительная логика для соединений внутри папки ----
                            // Если источник и цель находятся внутри ОДНОГО grid-cell-container (т.е. в одной папке)
                            // возможно, лучше рисовать вертикальные линии?
                            const sourceParentContainer = sourceElem.closest('.tree-grid-item'); // Внешний контейнер узла в сетке
                            const targetParentContainer = targetElem.closest('.tree-grid-item');

                            // Проверяем, находятся ли оба элемента внутри ОДНОЙ папки (сравнивая родительские контейнеры)
                            // Это эвристика, может потребовать уточнения
                            const sourceInFolder = sourceElem.closest('.folder-items-container');
                            const targetInFolder = targetElem.closest('.folder-items-container');

                            if (sourceInFolder && targetInFolder && sourceParentContainer === targetParentContainer) {
                                // Соединение ВНУТРИ папки (вероятно, вертикальное)
                                // Нижний центр source -> Верхний центр target
                                x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left;
                                y1 = sourceRect.bottom - svgRect.top;
                                x2 = targetRect.left + targetRect.width / 2 - svgRect.left;
                                y2 = targetRect.top - svgRect.top;
                                console.log(`Vertical connection inside folder: ${conn.sourceElementId} -> ${conn.targetElementId}`);
                            } else {
                                // Стандартное горизонтальное соединение между ячейками или из/в папку
                                console.log(`Horizontal connection: ${conn.sourceElementId} -> ${conn.targetElementId}`);
                            }
                            // ---- Конец дополнительной логики ----


                            line.setAttribute('x1', x1);
                            line.setAttribute('y1', y1);
                            line.setAttribute('x2', x2);
                            line.setAttribute('y2', y2);
                            line.style.visibility = 'visible'; // Показать линию

                        } catch (e) {
                            console.error("Error calculating line coordinates for conn:", conn, e);
                            if (line) line.style.visibility = 'hidden'; // Скрыть линию при ошибке
                        }
                    } else {
                        // console.warn("Elements not found or not visible for connection:", conn);
                        if (line) line.style.visibility = 'hidden'; // Скрыть линию, если элементы не найдены или не видимы
                    }
                });
            });
        });
    }
};