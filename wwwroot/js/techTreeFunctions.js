window.techTreeFunctions = {
    positionFolderPopup: function (popupId, targetNodeId, containerId) {
        requestAnimationFrame(() => {
            const popupElement = document.getElementById(popupId);
            const targetNodeElement = document.getElementById(targetNodeId);
            const containerElement = document.getElementById(containerId);

            if (!popupElement || !targetNodeElement || !containerElement) {
                console.error("Popup positioning error: Elements not found.",
                    { popupId, targetNodeId, containerId, popupElement, targetNodeElement, containerElement });
                if (popupElement) popupElement.style.visibility = 'hidden';
                return;
            }

            popupElement.style.visibility = 'hidden';
            popupElement.style.top = '-9999px';
            popupElement.style.left = '-9999px';
            popupElement.style.display = 'block';

            const popupHeight = popupElement.offsetHeight;
            const popupWidth = popupElement.offsetWidth;

            const targetRect = targetNodeElement.getBoundingClientRect();
            const containerRect = containerElement.getBoundingClientRect();
            const summaryPanel = document.getElementById('summary-container');
            const summaryPanelHeight = summaryPanel ? summaryPanel.offsetHeight : 0;

            const viewportHeight = window.innerHeight;
            const spaceBelow = viewportHeight - targetRect.bottom - summaryPanelHeight - 5;
            const spaceAbove = targetRect.top - 5;

            let top;
            const left = targetRect.left - containerRect.left + (containerElement.scrollLeft || 0);

            if (popupHeight <= spaceBelow) {
                top = targetRect.bottom - containerRect.top + (containerElement.scrollTop || 0) + 5;
            } else if (popupHeight <= spaceAbove) {
                top = targetRect.top - containerRect.top + (containerElement.scrollTop || 0) - popupHeight - 5;
            } else {
                top = targetRect.bottom - containerRect.top + (containerElement.scrollTop || 0) + 5;
                const availableHeight = viewportHeight - summaryPanelHeight - (targetRect.bottom + 5);
                if (availableHeight < popupHeight && availableHeight > 50) {
                }
                else {
                }
            }

            let finalLeft = left;
            if (containerRect.left + left + popupWidth > window.innerWidth - 10) {
                finalLeft = window.innerWidth - containerRect.left - popupWidth - 10 - (containerElement.scrollLeft || 0);
            }
            if (containerRect.left + finalLeft < 10) {
                finalLeft = 10 - containerRect.left + (containerElement.scrollLeft || 0);
            }

            popupElement.style.left = `${finalLeft}px`;
            popupElement.style.top = `${top}px`;
            popupElement.style.visibility = 'visible';
        });
    },

    copyTextToClipboard: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            console.log('Async: Copying to clipboard was successful!');
        }, function (err) {
            console.error('Async: Could not copy text: ', err);
            alert("Не удалось скопировать текст.");
        });
    },

    copyTextToClipboard_fallback: function (text) {
        try {
            const textArea = document.createElement("textarea");

            textArea.style.position = 'fixed';
            textArea.style.top = '-9999px';
            textArea.style.left = '-9999px';
            textArea.style.opacity = '0';

            textArea.value = text;
            document.body.appendChild(textArea);

            textArea.focus();
            textArea.select();

            const successful = document.execCommand('copy');

            document.body.removeChild(textArea);

            if (successful) {
                console.log('Copying to clipboard was successful!');
            } else {
                console.error('document.execCommand("copy") failed.');
                alert("Не удалось скопировать текст. Браузер не поддерживает или заблокировал команду.");
            }
        } catch (err) {
            console.error('Exception while copying text: ', err);
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
                    return;
                }
                const svgRect = svg.getBoundingClientRect();

                connectionData.forEach(conn => {
                    const line = svg.querySelector(`#${conn.lineId}`);
                    const sourceElem = document.getElementById(conn.sourceElementId);
                    const targetElem = document.getElementById(conn.targetElementId);

                    const isSourceVisible = sourceElem && sourceElem.offsetParent !== null;
                    const isTargetVisible = targetElem && targetElem.offsetParent !== null;

                    if (line && sourceElem && targetElem && isSourceVisible && isTargetVisible) {
                        try {
                            const sourceRect = sourceElem.getBoundingClientRect();
                            const targetRect = targetElem.getBoundingClientRect();

                            const sourceParentContainer = sourceElem.closest('.tree-grid-item');
                            const targetParentContainer = targetElem.closest('.tree-grid-item');
                            const sourceInFolder = sourceElem.closest('.folder-items-container');
                            const targetInFolder = targetElem.closest('.folder-items-container');

                            let x1, y1, x2, y2;

                            if (sourceInFolder && targetInFolder && sourceParentContainer === targetParentContainer) {
                                x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left;
                                y1 = sourceRect.bottom - svgRect.top;
                                x2 = targetRect.left + targetRect.width / 2 - svgRect.left;
                                y2 = targetRect.top - svgRect.top;
                            }
                            else {
                                x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left;
                                y1 = sourceRect.bottom - svgRect.top;
                                x2 = targetRect.left + targetRect.width / 2 - svgRect.left;
                                y2 = targetRect.top - svgRect.top;
                            }

                            line.setAttribute('x1', x1);
                            line.setAttribute('y1', y1);
                            line.setAttribute('x2', x2);
                            line.setAttribute('y2', y2);
                            line.style.visibility = 'visible';

                        } catch (e) {
                            if (line) line.style.visibility = 'hidden';
                        }
                    } else {
                        if (line) line.style.visibility = 'hidden';
                    }
                });
            });
        });
    },

    // Альтернативная функция поиска области по узлам техники
    calculateScreenshotAreaByNodes: function (selectedRanks) {
        console.log('=== USING ALTERNATIVE METHOD BY NODES ===');
        const treeGrid = document.querySelector('.tree-grid');
        if (!treeGrid) {
            console.error('Tree grid not found');
            return null;
        }

        const minRank = Math.min(...selectedRanks);
        const maxRank = Math.max(...selectedRanks);

        console.log(`Alternative method: looking for selected nodes in ranks ${minRank} to ${maxRank}`);

        // Ищем выбранные узлы (с классом node-selected) - включая те что в папках
        const selectedNodesInGrid = document.querySelectorAll('.tree-grid-item .node-selected');
        const selectedNodesInPopups = document.querySelectorAll('.folder-popup .node-selected');
        const allSelectedNodes = [...selectedNodesInGrid, ...selectedNodesInPopups];

        console.log(`Found ${selectedNodesInGrid.length} selected nodes in grid`);
        console.log(`Found ${selectedNodesInPopups.length} selected nodes in folder popups`);
        console.log(`Total selected nodes: ${allSelectedNodes.length}`);

        if (allSelectedNodes.length > 0) {
            // Если есть выбранные узлы, используем их для определения области
            const nodeRects = [];

            // Обрабатываем узлы в основной сетке
            selectedNodesInGrid.forEach(node => {
                const rect = node.getBoundingClientRect();
                const container = node.closest('.tree-grid-item');
                const isPremium = container && (container.classList.contains('tech-cat-premium') || container.classList.contains('tech-cat-event'));

                if (rect.width > 0 && rect.height > 0) {
                    nodeRects.push({
                        rect: rect,
                        isPremium: isPremium,
                        left: rect.left,
                        right: rect.right,
                        top: rect.top,
                        bottom: rect.bottom
                    });
                }
            });

            // Обрабатываем узлы в попапах папок
            selectedNodesInPopups.forEach(node => {
                const folderPopup = node.closest('.folder-popup');
                if (folderPopup) {
                    const folderId = folderPopup.id.replace('folder-popup-', '');
                    const correspondingFolder = document.querySelector(`#node-container-${folderId}`);
                    if (correspondingFolder) {
                        const folderRect = correspondingFolder.getBoundingClientRect();
                        const isPremium = correspondingFolder.classList.contains('tech-cat-premium') ||
                            correspondingFolder.classList.contains('tech-cat-event');

                        if (folderRect.width > 0 && folderRect.height > 0) {
                            nodeRects.push({
                                rect: folderRect, // Используем позицию папки в сетке
                                isPremium: isPremium,
                                left: folderRect.left,
                                right: folderRect.right,
                                top: folderRect.top,
                                bottom: folderRect.bottom
                            });
                            console.log(`Added folder ${folderId} to screenshot area (premium: ${isPremium})`);
                        }
                    }
                }
            });

            if (nodeRects.length > 0) {
                const treeRect = treeGrid.getBoundingClientRect();

                // Находим границы выбранных узлов
                const minTop = Math.min(...nodeRects.map(r => r.top));
                const maxBottom = Math.max(...nodeRects.map(r => r.bottom));

                // Определяем горизонтальные границы
                let minLeft = Math.min(...nodeRects.map(r => r.left));
                let maxRight = Math.max(...nodeRects.map(r => r.right));

                // Проверяем, есть ли выбранная премиумная техника
                const hasPremiumSelected = nodeRects.some(r => r.isPremium);

                if (!hasPremiumSelected) {
                    // Если премиумная техника не выбрана, ограничиваем область только стандартной техникой
                    console.log('No premium vehicles selected, limiting to standard area');

                    // Ищем разделитель между стандартной и премиумной техникой
                    const premiumDivider = document.querySelector('.premium-divider-v');
                    if (premiumDivider) {
                        const dividerRect = premiumDivider.getBoundingClientRect();
                        // Ограничиваем правую границу разделителем
                        maxRight = Math.min(maxRight, dividerRect.left);
                        console.log('Limited screenshot width to exclude premium area');
                    }
                }

                const area = {
                    x: Math.max(0, minLeft - treeRect.left - 20), // Небольшой отступ слева
                    y: Math.max(0, minTop - treeRect.top - 50), // Отступ сверху для меток ранга
                    width: Math.max(200, (maxRight - minLeft) + 40), // Отступы слева и справа
                    height: Math.max(200, (maxBottom - minTop) + 100) // Отступы сверху и снизу
                };

                console.log('Alternative screenshot area by selected nodes:', area);
                console.log('Premium vehicles included:', hasPremiumSelected);
                return area;
            }
        }

        // Fallback: используем все узлы техники, но только стандартные если нет выбранной премиумной
        console.log('Fallback: using all visible nodes');
        const allNodes = document.querySelectorAll('.tree-grid-item');
        const rankedNodes = [];

        allNodes.forEach(node => {
            const nodeRect = node.getBoundingClientRect();
            if (nodeRect.width > 0 && nodeRect.height > 0) {
                const isPremium = node.classList.contains('tech-cat-premium') || node.classList.contains('tech-cat-event');
                rankedNodes.push({
                    element: node,
                    rect: nodeRect,
                    top: nodeRect.top,
                    isPremium: isPremium
                });
            }
        });

        if (rankedNodes.length === 0) {
            console.log('No nodes found, returning null');
            return null;
        }

        // Фильтруем только стандартные узлы для fallback
        const standardNodes = rankedNodes.filter(node => !node.isPremium);
        const nodesToUse = standardNodes.length > 0 ? standardNodes : rankedNodes;

        // Сортируем узлы по вертикальной позиции
        nodesToUse.sort((a, b) => a.top - b.top);

        console.log(`Found ${nodesToUse.length} nodes for fallback (${standardNodes.length} standard, ${rankedNodes.length - standardNodes.length} premium)`);

        const treeRect = treeGrid.getBoundingClientRect();
        const firstNode = nodesToUse[Math.floor(nodesToUse.length * 0.3)]; // Начинаем с ~30% от начала
        const lastNode = nodesToUse[Math.floor(nodesToUse.length * 0.9)];   // Заканчиваем на ~90%

        // Определяем горизонтальные границы
        const allLefts = nodesToUse.map(n => n.rect.left);
        const allRights = nodesToUse.map(n => n.rect.right);

        // Используем область только стандартной техники
        const area = {
            x: Math.max(0, Math.min(...allLefts) - treeRect.left - 20),
            y: Math.max(0, firstNode.rect.top - treeRect.top - 50),
            width: Math.max(200, (Math.max(...allRights) - Math.min(...allLefts)) + 40),
            height: Math.max(200, (lastNode.rect.bottom - firstNode.rect.top) + 100)
        };

        console.log('Alternative screenshot area by node estimation (standard only):', area);
        return area;
    },

    // Новая функция для определения области скриншота по рангам (вся область, исключая премиумную)
    calculateScreenshotAreaByRanks: function (selectedRanks) {
        console.log('=== CALCULATE SCREENSHOT AREA BY RANKS (FULL AREA) ===');
        const treeGrid = document.querySelector('.tree-grid');
        if (!treeGrid) {
            console.error('Tree grid not found');
            return null;
        }

        if (!selectedRanks || selectedRanks.length === 0) {
            console.error('No ranks selected');
            return null;
        }

        const minRank = Math.min(...selectedRanks);
        const maxRank = Math.max(...selectedRanks);

        console.log(`Screenshot area for full ranks ${minRank} to ${maxRank} (excluding premium)`);

        // Находим элементы рангов - ищем более точно
        const allRankLabels = document.querySelectorAll('.rank-label');
        console.log(`Found ${allRankLabels.length} rank labels total`);

        let topElement = null;
        let bottomElement = null;
        const foundRanks = [];
        const rankElementMap = new Map(); // Карта ранг -> элемент

        allRankLabels.forEach(label => {
            const rankText = label.textContent.trim();
            console.log(`Checking rank label: "${rankText}"`);

            // Более умный парсинг - ищем "Ранг" потом первое число от 1 до 8
            const rankMatch = rankText.match(/Ранг\s+(\d+)/);
            if (rankMatch) {
                const rank = parseInt(rankMatch[1]);

                // Проверяем что это реальный ранг War Thunder (1-8)
                if (rank >= 1 && rank <= 8) {
                    foundRanks.push(rank);
                    rankElementMap.set(rank, label);
                    console.log(`Found valid rank ${rank} in text: "${rankText}"`);
                } else {
                    // Проверяем, может это составное число - например "12" = "1" + "2"
                    const rankStr = rankMatch[1];
                    if (rankStr.length === 2) {
                        const firstDigit = parseInt(rankStr[0]);
                        if (firstDigit >= 1 && firstDigit <= 8) {
                            foundRanks.push(firstDigit);
                            rankElementMap.set(firstDigit, label);
                            console.log(`Found valid rank ${firstDigit} from compound number ${rank} in text: "${rankText}"`);
                        } else {
                            console.log(`Skipped invalid compound rank ${rank} (first digit ${firstDigit} not in range 1-8)`);
                        }
                    } else {
                        console.log(`Skipped invalid rank ${rank} (not in range 1-8)`);
                    }
                }
            } else {
                console.log(`No rank pattern found in: "${rankText}"`);
            }
        });

        console.log(`Available ranks: [${foundRanks.join(', ')}]`);
        console.log(`Looking for ranks: ${minRank} to ${maxRank}`);

        // Дополнительная отладочная информация
        console.log('Rank element mapping:');
        foundRanks.forEach(rank => {
            const element = rankElementMap.get(rank);
            const elementText = element ? element.textContent.trim() : 'NOT FOUND';
            console.log(`  Rank ${rank}: "${elementText}"`);
        });

        // СТРОГО ищем только нужные ранги - НЕ ИСПОЛЬЗУЕМ FALLBACK
        if (rankElementMap.has(minRank)) {
            topElement = rankElementMap.get(minRank);
            console.log(`Found top element for rank ${minRank}`);
        }

        if (rankElementMap.has(maxRank)) {
            bottomElement = rankElementMap.get(maxRank);
            console.log(`Found bottom element for rank ${maxRank}`);
        }

        // Если это один и тот же ранг, используем один элемент для обеих границ
        if (minRank === maxRank && topElement) {
            bottomElement = topElement;
            console.log(`Single rank ${minRank} - using same element for both boundaries`);
        }

        // Если диапазон рангов, но найден только один из концов - попробуем расширить до доступных рангов
        if (!topElement && bottomElement && foundRanks.length > 0) {
            // Если нет начального ранга, найдем ближайший меньший доступный ранг
            const availableLowerRanks = foundRanks.filter(r => r <= maxRank).sort((a, b) => b - a);
            if (availableLowerRanks.length > 0) {
                const nearestLowerRank = availableLowerRanks[0];
                topElement = rankElementMap.get(nearestLowerRank);
                console.log(`Using nearest lower rank ${nearestLowerRank} as top element`);
            }
        }

        if (!bottomElement && topElement && foundRanks.length > 0) {
            // Если нет конечного ранга, найдем ближайший больший доступный ранг
            const availableHigherRanks = foundRanks.filter(r => r >= minRank).sort((a, b) => a - b);
            if (availableHigherRanks.length > 0) {
                const nearestHigherRank = availableHigherRanks[0];
                bottomElement = rankElementMap.get(nearestHigherRank);
                console.log(`Using nearest higher rank ${nearestHigherRank} as bottom element`);
            }
        }

        // КРИТИЧЕСКИ ВАЖНО: если не найдены нужные ранги, возвращаем null
        if (!topElement || !bottomElement) {
            console.error(`CRITICAL: Could not find required rank elements!`);
            console.error(`Requested ranks: ${minRank} to ${maxRank}`);
            console.error(`Available ranks: [${foundRanks.join(', ')}]`);
            console.error(`Top element found: ${!!topElement} (for rank ${minRank})`);
            console.error(`Bottom element found: ${!!bottomElement} (for rank ${maxRank})`);

            // Если доступен хотя бы один ранг из диапазона, используем его
            const availableInRange = foundRanks.filter(r => r >= minRank && r <= maxRank);
            if (availableInRange.length > 0) {
                console.log(`Found ${availableInRange.length} ranks in requested range: [${availableInRange.join(', ')}]`);
                const singleRank = availableInRange[0];
                const singleElement = rankElementMap.get(singleRank);
                if (singleElement) {
                    topElement = singleElement;
                    bottomElement = singleElement;
                    console.log(`Using single available rank ${singleRank} for screenshot`);
                }
            }

            // Если все еще ничего не найдено - возвращаем null
            if (!topElement || !bottomElement) {
                return null;
            }
        }

        const treeRect = treeGrid.getBoundingClientRect();
        const topRect = topElement.getBoundingClientRect();
        const bottomRect = bottomElement.getBoundingClientRect();

        console.log('Tree rect:', treeRect);
        console.log('Top element rect:', topRect);
        console.log('Bottom element rect:', bottomRect);

        // Определяем правильную границу стандартной техники
        let width = treeRect.width;
        const premiumDivider = document.querySelector('.premium-divider-v');

        if (premiumDivider) {
            const dividerRect = premiumDivider.getBoundingClientRect();
            const dividerPosition = dividerRect.left - treeRect.left;
            // Добавляем небольшой отступ чтобы не обрезать последний столбец стандартной техники
            width = Math.max(200, dividerPosition - 10);
            console.log('Limited screenshot width to exclude premium area:', width, 'divider at:', dividerPosition);
        } else {
            // Если нет разделителя, попробуем найти границу по классам техники
            const standardNodes = document.querySelectorAll('.tree-grid-item.tech-cat-standard');
            const premiumNodes = document.querySelectorAll('.tree-grid-item.tech-cat-premium, .tree-grid-item.tech-cat-event');

            if (standardNodes.length > 0 && premiumNodes.length > 0) {
                const standardRights = Array.from(standardNodes).map(node => {
                    const rect = node.getBoundingClientRect();
                    return rect.right - treeRect.left;
                });
                const maxStandardRight = Math.max(...standardRights);
                width = maxStandardRight + 20; // Небольшой отступ
                console.log('No divider found, calculated width by nodes:', width);
            }
        }

        // Вычисляем координаты области для обрезки
        const area = {
            x: 0, // Начинаем с левого края сетки
            y: Math.max(0, topRect.top - treeRect.top - 10), // Небольшой отступ сверху
            width: Math.min(width, treeRect.width), // Ограничиваем шириной сетки
            height: Math.max(50, (bottomRect.bottom - topRect.top) + 20) // Отступ снизу
        };

        console.log(`SUCCESS: Calculated screenshot area for ranks ${minRank}-${maxRank}:`, area);
        return area;
    },

    // УСТАРЕВШАЯ функция - оставлена для совместимости
    calculateScreenshotArea: function (selectedRanks) {
        console.log('=== USING DEPRECATED calculateScreenshotArea - redirecting to new function ===');
        return this.calculateScreenshotAreaByRanks(selectedRanks);
    },

    // Функция создания скриншота
    createScreenshot: async function (selectedRanks, filename, onProgress) {
        try {
            console.log('Starting screenshot creation for ranks:', selectedRanks);

            if (onProgress) onProgress('Подготовка области...');

            // Находим контейнер и панель итогов
            const treeContainer = document.getElementById('tech-tree-container-id');
            const summaryContainer = document.getElementById('summary-container');

            if (!treeContainer) {
                throw new Error('Tech tree container not found');
            }

            console.log('Tree container found:', treeContainer);
            console.log('Summary container found:', summaryContainer);

            // Создаем временный контейнер для скриншота
            const tempContainer = document.createElement('div');
            tempContainer.style.position = 'absolute';
            tempContainer.style.top = '-10000px';
            tempContainer.style.left = '-10000px';
            tempContainer.style.background = '#1a1a1a';
            tempContainer.style.width = treeContainer.scrollWidth + 'px';
            tempContainer.style.overflow = 'visible';

            // Клонируем дерево техники
            const treeClone = treeContainer.cloneNode(true);
            treeClone.style.paddingBottom = '10px';
            treeClone.style.position = 'relative';
            tempContainer.appendChild(treeClone);

            // Если есть панель итогов, добавляем ее
            if (summaryContainer) {
                const summaryClone = summaryContainer.cloneNode(true);

                // Убираем ненужные элементы из клона панели
                const cloneNamesContainer = summaryClone.querySelector('#selected-names-container');
                const cloneButtons = summaryClone.querySelectorAll('button');

                if (cloneNamesContainer) {
                    cloneNamesContainer.remove();
                    console.log('Removed names container from summary clone');
                }

                cloneButtons.forEach(btn => {
                    btn.remove();
                    console.log('Removed button from summary clone');
                });

                // Настраиваем стили панели для скриншота
                summaryClone.style.position = 'relative';
                summaryClone.style.background = 'rgba(40, 40, 40, 0.95)';
                summaryClone.style.backdropFilter = 'blur(3px)';
                summaryClone.style.borderTop = '1px solid #666';
                summaryClone.style.marginTop = '10px';
                summaryClone.style.padding = '8px 20px';
                summaryClone.style.display = 'flex';
                summaryClone.style.flexDirection = 'column';
                summaryClone.style.alignItems = 'center';
                summaryClone.style.gap = '8px';
                summaryClone.style.width = treeContainer.scrollWidth + 'px';

                tempContainer.appendChild(summaryClone);
                console.log('Added summary panel to temp container');
            }

            // Добавляем временный контейнер в документ
            document.body.appendChild(tempContainer);
            console.log('Temp container added to document');

            // Ждем немного чтобы браузер отрендерил элементы
            await new Promise(resolve => setTimeout(resolve, 100));

            if (onProgress) onProgress('Создание скриншота...');

            // Создаем скриншот временного контейнера
            const canvas = await html2canvas(tempContainer, {
                scale: 3,
                useCORS: true,
                allowTaint: true,
                backgroundColor: '#1a1a1a',
                logging: true,
                width: tempContainer.scrollWidth,
                height: tempContainer.scrollHeight,
                scrollX: 0,
                scrollY: 0
            });

            console.log('Screenshot canvas created:', {
                width: canvas.width,
                height: canvas.height
            });

            // Удаляем временный контейнер
            if (tempContainer && tempContainer.parentNode) {
                document.body.removeChild(tempContainer);
                console.log('Temp container removed');
            } else {
                console.log('Temp container already removed or not found');
            }

            if (onProgress) onProgress('Обработка изображения...');

            // Получаем область для обрезки по рангам (без премиумной части)
            const screenshotArea = this.calculateScreenshotAreaByRanks(selectedRanks);
            if (!screenshotArea) {
                console.error('CRITICAL: Cannot create screenshot - required ranks not found!');
                console.error('Selected ranks:', selectedRanks);

                // Удаляем временный контейнер при ошибке
                if (tempContainer && tempContainer.parentNode) {
                    document.body.removeChild(tempContainer);
                }

                throw new Error(`Не удалось найти ранги ${selectedRanks.join(', ')} на странице. Доступные ранги: проверьте консоль для подробностей.`);
            }

            console.log('Screenshot area to crop:', screenshotArea);

            // Проверяем размер области - если слишком большая, ограничиваем
            const maxWidth = 4000;
            const maxHeight = 6000;

            if (screenshotArea.width > maxWidth || screenshotArea.height > maxHeight) {
                console.warn(`Screenshot area too large: ${screenshotArea.width}x${screenshotArea.height}, limiting to ${maxWidth}x${maxHeight}`);
                screenshotArea.width = Math.min(screenshotArea.width, maxWidth);
                screenshotArea.height = Math.min(screenshotArea.height, maxHeight);
            }

            // Создаем новый canvas для обрезанного изображения
            const croppedCanvas = document.createElement('canvas');
            const ctx = croppedCanvas.getContext('2d');

            // Учитываем масштаб при обрезке и добавляем место для панели итогов
            const scale = 3;
            const summaryPanelHeight = summaryContainer ? 60 : 0; // Примерная высота панели

            const cropX = Math.max(0, screenshotArea.x * scale);
            const cropY = Math.max(0, screenshotArea.y * scale);
            const cropWidth = Math.min(canvas.width - cropX, screenshotArea.width * scale);
            const cropHeight = Math.min(canvas.height - cropY, (screenshotArea.height + summaryPanelHeight) * scale);

            // Проверяем финальный размер canvas
            const maxCanvasSize = 16384; // Максимальный размер canvas в большинстве браузеров
            if (cropWidth > maxCanvasSize || cropHeight > maxCanvasSize) {
                console.warn(`Final canvas size too large: ${cropWidth}x${cropHeight}, reducing scale`);
                const scaleReduction = Math.min(maxCanvasSize / cropWidth, maxCanvasSize / cropHeight, 1);
                croppedCanvas.width = Math.floor(cropWidth * scaleReduction);
                croppedCanvas.height = Math.floor(cropHeight * scaleReduction);
            } else {
                croppedCanvas.width = cropWidth;
                croppedCanvas.height = cropHeight;
            }

            console.log('Cropping parameters:', {
                scale,
                cropX,
                cropY,
                cropWidth,
                cropHeight,
                summaryPanelHeight,
                finalCanvasSize: {
                    width: croppedCanvas.width,
                    height: croppedCanvas.height
                }
            });

            // Обрезаем изображение с учетом возможного масштабирования
            if (croppedCanvas.width < cropWidth || croppedCanvas.height < cropHeight) {
                // Если размер был уменьшен, масштабируем изображение
                ctx.drawImage(
                    canvas,
                    cropX, cropY, cropWidth, cropHeight,
                    0, 0, croppedCanvas.width, croppedCanvas.height
                );
                console.log('Screenshot scaled down due to size limitations');
            } else {
                // Обычная обрезка без масштабирования
                ctx.drawImage(
                    canvas,
                    cropX, cropY, cropWidth, cropHeight,
                    0, 0, cropWidth, cropHeight
                );
            }

            console.log('Screenshot created successfully');
            return croppedCanvas;

        } catch (error) {
            console.error('Error creating screenshot:', error);

            // Безопасно удаляем временный контейнер при ошибке
            try {
                if (tempContainer && tempContainer.parentNode) {
                    document.body.removeChild(tempContainer);
                    console.log('Temp container removed in error handler');
                }
            } catch (cleanupError) {
                console.log('Temp container was already removed or not found during cleanup');
            }

            throw error;
        }
    },

    // Функция копирования скриншота в буфер обмена с поддержкой HTTP
    copyScreenshotToClipboard: async function (selectedRanks, progressCallbackRef, completeCallbackRef) {
        try {
            const canvas = await this.createScreenshot(selectedRanks, null, (message) => {
                if (progressCallbackRef) {
                    progressCallbackRef.invokeMethodAsync('OnScreenshotProgress', message);
                }
            });

            if (progressCallbackRef) {
                progressCallbackRef.invokeMethodAsync('OnScreenshotProgress', 'Копирование в буфер обмена...');
            }

            canvas.toBlob(async (blob) => {
                try {
                    // Проверяем поддержку Clipboard API
                    if (navigator.clipboard && navigator.clipboard.write) {
                        // Современный способ для HTTPS
                        await navigator.clipboard.write([
                            new ClipboardItem({
                                'image/png': blob
                            })
                        ]);

                        console.log('Screenshot copied to clipboard successfully (modern API)');
                        if (completeCallbackRef) {
                            completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', true, 'Скриншот скопирован в буфер обмена');
                        }
                    } else {
                        // Fallback для HTTP - создаем временную ссылку для скачивания
                        console.log('Clipboard API not available (HTTP), offering download instead');

                        const url = URL.createObjectURL(blob);
                        const link = document.createElement('a');
                        link.download = this.generateScreenshotFilename();
                        link.href = url;

                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);

                        setTimeout(() => URL.revokeObjectURL(url), 1000);

                        if (completeCallbackRef) {
                            completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', true, 'Скриншот сохранен (буфер обмена недоступен на HTTP)');
                        }
                    }
                } catch (err) {
                    console.error('Error copying to clipboard:', err);
                    if (completeCallbackRef) {
                        completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', false, 'Не удалось скопировать в буфер обмена: ' + err.message);
                    }
                }
            }, 'image/png');

        } catch (error) {
            console.error('Error in copyScreenshotToClipboard:', error);
            if (completeCallbackRef) {
                completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', false, 'Ошибка создания скриншота: ' + error.message);
            }
        }
    },

    // Функция скачивания скриншота
    downloadScreenshot: async function (selectedRanks, filename, progressCallbackRef, completeCallbackRef) {
        try {
            const canvas = await this.createScreenshot(selectedRanks, filename, (message) => {
                if (progressCallbackRef) {
                    progressCallbackRef.invokeMethodAsync('OnScreenshotProgress', message);
                }
            });

            if (progressCallbackRef) {
                progressCallbackRef.invokeMethodAsync('OnScreenshotProgress', 'Подготовка загрузки...');
            }

            canvas.toBlob((blob) => {
                try {
                    const url = URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.download = filename || 'screenshot.png';
                    link.href = url;

                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                    // Освобождаем память
                    setTimeout(() => URL.revokeObjectURL(url), 1000);

                    console.log('Screenshot download initiated successfully');
                    if (completeCallbackRef) {
                        completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', true, 'Скриншот сохранен');
                    }
                } catch (err) {
                    console.error('Error downloading screenshot:', err);
                    if (completeCallbackRef) {
                        completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', false, 'Ошибка загрузки: ' + err.message);
                    }
                }
            }, 'image/png');

        } catch (error) {
            console.error('Error in downloadScreenshot:', error);
            if (completeCallbackRef) {
                completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', false, 'Ошибка создания скриншота: ' + error.message);
            }
        }
    },

    // Функция для генерации имени файла на основе URL
    generateScreenshotFilename: function () {
        try {
            const url = new URL(window.location.href);
            const pathParts = url.pathname.split('/').filter(p => p);

            // Извлекаем нацию и тип техники из URL
            let filename = 'screenshot';
            if (pathParts.length >= 3 && pathParts[0] === 'tree') {
                const nation = pathParts[1];
                const vehicleType = pathParts[2];
                filename = `tree-${nation}-${vehicleType}`;
            }

            // Добавляем параметры выбора, если они есть
            const params = url.searchParams;
            const selected = params.get('selected');
            if (selected) {
                filename += '-selected';
            }

            // Добавляем временную метку для уникальности
            const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
            filename += `-${timestamp}.png`;

            return filename;
        } catch (error) {
            console.error('Error generating filename:', error);
            return `screenshot-${Date.now()}.png`;
        }
    }
};