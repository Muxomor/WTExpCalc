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
    createScreenshot4K: async function (selectedRanks, filename, onProgress) {
        try {
            console.log('Starting 4K screenshot creation for ranks:', selectedRanks);

            if (onProgress) onProgress('Подготовка 4K области...');

            const treeContainer = document.getElementById('tech-tree-container-id');
            const summaryContainer = document.getElementById('summary-container');

            if (!treeContainer) {
                throw new Error('Tech tree container not found');
            }

            console.log('Tree container found for 4K screenshot');

            const baseScreenshotArea = this.calculateScreenshotAreaByRanks(selectedRanks);
            if (!baseScreenshotArea) {
                throw new Error(`Не удалось найти ранги ${selectedRanks.join(', ')} на странице.`);
            }

            const { foundRanks } = this.findRankElements();
            const maxAvailableRank = Math.max(...foundRanks);
            const maxSelectedRank = Math.max(...selectedRanks);
            const treeGridRect = document.querySelector('.tree-grid').getBoundingClientRect();

            const summaryRect = summaryContainer ? summaryContainer.getBoundingClientRect() : null;
            const viewportHeight = window.innerHeight;

            let distanceToSummary = Infinity;
            if (summaryRect) {
                const areaBottomInViewport = baseScreenshotArea.y + baseScreenshotArea.height - treeGridRect.top + treeGridRect.top;
                distanceToSummary = Math.abs(summaryRect.top - areaBottomInViewport);
            }

            const isMaxAvailableRank = maxSelectedRank >= maxAvailableRank;
            const isCloseToSummaryPanel = distanceToSummary < viewportHeight * 0.4;
            const useOldMethod = isMaxAvailableRank || isCloseToSummaryPanel;

            const tempContainer = document.createElement('div');
            tempContainer.style.position = 'absolute';
            tempContainer.style.top = '-10000px';
            tempContainer.style.left = '-10000px';
            tempContainer.style.background = '#1a1a1a';
            tempContainer.style.width = treeContainer.scrollWidth + 'px';
            tempContainer.style.overflow = 'visible';

            const treeClone = treeContainer.cloneNode(true);
            treeClone.style.paddingBottom = '10px';
            treeClone.style.position = 'relative';
            tempContainer.appendChild(treeClone);

            let finalScreenshotArea;

            if (useOldMethod) {
                if (summaryContainer) {
                    const summaryClone = summaryContainer.cloneNode(true);
                    const cloneNamesContainer = summaryClone.querySelector('#selected-names-container');
                    const cloneButtons = summaryClone.querySelectorAll('button');

                    if (cloneNamesContainer) cloneNamesContainer.remove();
                    cloneButtons.forEach(btn => btn.remove());

                    summaryClone.style.position = 'relative';
                    summaryClone.style.background = 'rgba(40, 40, 40, 0.95)';
                    summaryClone.style.backdropFilter = 'blur(3px)';
                    summaryClone.style.borderTop = '1px solid #666';
                    summaryClone.style.marginTop = '20px';
                    summaryClone.style.padding = '12px 20px';
                    summaryClone.style.display = 'flex';
                    summaryClone.style.flexDirection = 'column';
                    summaryClone.style.alignItems = 'center';
                    summaryClone.style.gap = '8px';
                    summaryClone.style.width = treeContainer.scrollWidth + 'px';

                    tempContainer.appendChild(summaryClone);
                }

                const realSummaryHeight = summaryContainer ? summaryContainer.getBoundingClientRect().height : 80;
                const additionalPadding = 50;
                const panelHeight = realSummaryHeight + additionalPadding;

                finalScreenshotArea = {
                    x: baseScreenshotArea.x,
                    y: baseScreenshotArea.y,
                    width: baseScreenshotArea.width,
                    height: baseScreenshotArea.height + panelHeight
                };
            } else {
                if (summaryContainer) {
                    const summaryClone = summaryContainer.cloneNode(true);
                    const cloneNamesContainer = summaryClone.querySelector('#selected-names-container');
                    const cloneButtons = summaryClone.querySelectorAll('button');

                    if (cloneNamesContainer) cloneNamesContainer.remove();
                    cloneButtons.forEach(btn => btn.remove());

                    summaryClone.style.position = 'absolute';
                    summaryClone.style.background = 'rgba(40, 40, 40, 0.95)';
                    summaryClone.style.backdropFilter = 'blur(3px)';
                    summaryClone.style.border = '1px solid #666';
                    summaryClone.style.borderRadius = '8px';
                    summaryClone.style.padding = '12px 20px';
                    summaryClone.style.display = 'flex';
                    summaryClone.style.flexDirection = 'column';
                    summaryClone.style.alignItems = 'center';
                    summaryClone.style.gap = '8px';
                    summaryClone.style.zIndex = '1000';
                    summaryClone.style.boxShadow = '0 4px 12px rgba(0,0,0,0.5)';

                    const panelWidth = 300;
                    const panelHeight = 80;
                    const margin = 20;

                    const panelX = Math.max(baseScreenshotArea.x + baseScreenshotArea.width - panelWidth - margin, baseScreenshotArea.x + margin);
                    const panelY = baseScreenshotArea.y + baseScreenshotArea.height + margin;

                    summaryClone.style.left = panelX + 'px';
                    summaryClone.style.top = panelY + 'px';
                    summaryClone.style.width = panelWidth + 'px';

                    treeClone.appendChild(summaryClone);
                }

                const panelHeight = 100;
                const panelMargin = 30;
                finalScreenshotArea = {
                    x: baseScreenshotArea.x,
                    y: baseScreenshotArea.y,
                    width: baseScreenshotArea.width,
                    height: baseScreenshotArea.height + panelHeight + panelMargin
                };
            }

            document.body.appendChild(tempContainer);
            await new Promise(resolve => setTimeout(resolve, 100));

            if (onProgress) onProgress('Создание 4K скриншота...');

            const canvas = await html2canvas(tempContainer, {
                scale: 4, // Увеличиваем scale для лучшего качества
                useCORS: true,
                allowTaint: true,
                backgroundColor: '#1a1a1a',
                logging: true,
                width: tempContainer.scrollWidth,
                height: tempContainer.scrollHeight,
                scrollX: 0,
                scrollY: 0
            });

            if (tempContainer && tempContainer.parentNode) {
                document.body.removeChild(tempContainer);
            }

            if (onProgress) onProgress('Обработка 4K изображения...');

            const croppedCanvas = document.createElement('canvas');
            const ctx = croppedCanvas.getContext('2d');

            const scale = 4;
            const cropX = Math.max(0, finalScreenshotArea.x * scale);
            const cropY = Math.max(0, finalScreenshotArea.y * scale);
            const cropWidth = Math.min(canvas.width - cropX, finalScreenshotArea.width * scale);
            const cropHeight = Math.min(canvas.height - cropY, finalScreenshotArea.height * scale);

            const MAX_4K_SIZE = 4000;
            let finalWidth = cropWidth;
            let finalHeight = cropHeight;

            if (cropWidth > MAX_4K_SIZE || cropHeight > MAX_4K_SIZE) {
                const scaleDown = Math.min(MAX_4K_SIZE / cropWidth, MAX_4K_SIZE / cropHeight);
                finalWidth = Math.floor(cropWidth * scaleDown);
                finalHeight = Math.floor(cropHeight * scaleDown);
                console.log(`4K limit applied: scaled from ${cropWidth}x${cropHeight} to ${finalWidth}x${finalHeight}`);
            } else {
                console.log(`4K limit not needed: image is ${cropWidth}x${cropHeight}`);
            }

            croppedCanvas.width = finalWidth;
            croppedCanvas.height = finalHeight;

            ctx.drawImage(
                canvas,
                cropX, cropY, cropWidth, cropHeight,
                0, 0, finalWidth, finalHeight
            );

            console.log(`4K screenshot created: ${finalWidth}x${finalHeight}`);
            return croppedCanvas;

        } catch (error) {
            console.error('Error creating 4K screenshot:', error);

            try {
                if (tempContainer && tempContainer.parentNode) {
                    document.body.removeChild(tempContainer);
                }
            } catch (cleanupError) {
                console.log('Temp container cleanup failed or already removed');
            }

            throw error;
        }
    },

    downloadScreenshot4K: async function (selectedRanks, filename, progressCallbackRef, completeCallbackRef) {
        try {
            const canvas = await this.createScreenshot4K(selectedRanks, filename, (message) => {
                if (progressCallbackRef) {
                    progressCallbackRef.invokeMethodAsync('OnScreenshotProgress', message);
                }
            });

            if (progressCallbackRef) {
                progressCallbackRef.invokeMethodAsync('OnScreenshotProgress', 'Подготовка 4K загрузки...');
            }

            canvas.toBlob((blob) => {
                try {
                    const url = URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.download = filename || 'screenshot-4k.png';
                    link.href = url;

                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                    setTimeout(() => URL.revokeObjectURL(url), 1000);

                    console.log('4K screenshot download initiated successfully');
                    if (completeCallbackRef) {
                        completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', true, '4K скриншот сохранен');
                    }
                } catch (err) {
                    console.error('Error downloading 4K screenshot:', err);
                    if (completeCallbackRef) {
                        completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', false, 'Ошибка загрузки 4K: ' + err.message);
                    }
                }
            }, 'image/png');

        } catch (error) {
            console.error('Error in downloadScreenshot4K:', error);
            if (completeCallbackRef) {
                completeCallbackRef.invokeMethodAsync('OnScreenshotComplete', false, 'Ошибка создания 4K скриншота: ' + error.message);
            }
        }
    },
    generateScreenshotFilename4K: function () {
        try {
            const url = new URL(window.location.href);
            const pathParts = url.pathname.split('/').filter(p => p);

            let filename = 'screenshot-4k';
            if (pathParts.length >= 3 && pathParts[0] === 'tree') {
                const nation = pathParts[1];
                const vehicleType = pathParts[2];
                filename = `tree-${nation}-${vehicleType}-4k`;
            }

            const params = url.searchParams;
            const selected = params.get('selected');
            if (selected) {
                filename += '-selected';
            }

            const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
            filename += `-${timestamp}.png`;

            return filename;
        } catch (error) {
            console.error('Error generating 4K filename:', error);
            return `screenshot-4k-${Date.now()}.png`;
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
    findRankElements: function () {
        const allRankLabels = document.querySelectorAll('.rank-label');
        const foundRanks = [];
        const rankElementMap = new Map();

        console.log(`Found ${allRankLabels.length} rank labels total`);

        allRankLabels.forEach(label => {
            const rankText = label.textContent.trim();
            console.log(`Checking rank label: "${rankText}"`);

            const rankPatterns = [
                /^Ранг\s+(\d+)(?:\s|$)/i,

                /^Rank\s+(\d+)(?:\s|$)/i,

                /^Rank\s+(\d)(\d+)/i, 
                /^Ранг\s+(\d)(\d+)/i, 

                /(?:Rank|Ранг)\s+(\d+)/i  
            ];

            let rank = null;

            for (const pattern of rankPatterns) {
                const match = rankText.match(pattern);
                if (match) {
                    let foundNumber = parseInt(match[1]);

                    if (foundNumber > 8) {
                        const firstDigit = parseInt(foundNumber.toString()[0]);
                        if (firstDigit >= 1 && firstDigit <= 8) {
                            rank = firstDigit;
                            console.log(`Extracted rank ${rank} from compound number ${foundNumber} in: "${rankText}"`);
                            break;
                        }
                    } else if (foundNumber >= 1 && foundNumber <= 8) {
                        rank = foundNumber;
                        console.log(`Found direct rank ${rank} in: "${rankText}"`);
                        break;
                    }
                }
            }

            if (rank && rank >= 1 && rank <= 8) {
                if (!rankElementMap.has(rank)) {
                    foundRanks.push(rank);
                    rankElementMap.set(rank, label);
                    console.log(`Successfully mapped rank ${rank} to element with text: "${rankText}"`);
                } else {
                    console.log(`Rank ${rank} already mapped, skipping duplicate from: "${rankText}"`);
                }
            } else {
                console.log(`No valid rank found in: "${rankText}"`);
            }
        });

        foundRanks.sort((a, b) => a - b);

        console.log(`Final result: Found ranks [${foundRanks.join(', ')}]`);
        console.log('Rank mappings:');
        foundRanks.forEach(rank => {
            const element = rankElementMap.get(rank);
            console.log(`  Rank ${rank}: "${element.textContent.trim()}"`);
        });

        return { foundRanks, rankElementMap };
    },
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

        const selectedNodesInGrid = document.querySelectorAll('.tree-grid-item .node-selected');
        const selectedNodesInPopups = document.querySelectorAll('.folder-popup .node-selected');
        const allSelectedNodes = [...selectedNodesInGrid, ...selectedNodesInPopups];

        console.log(`Found ${selectedNodesInGrid.length} selected nodes in grid`);
        console.log(`Found ${selectedNodesInPopups.length} selected nodes in folder popups`);
        console.log(`Total selected nodes: ${allSelectedNodes.length}`);

        if (allSelectedNodes.length > 0) {
            const nodeRects = [];

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
                                rect: folderRect, 
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

                const minTop = Math.min(...nodeRects.map(r => r.top));
                const maxBottom = Math.max(...nodeRects.map(r => r.bottom));

                let minLeft = Math.min(...nodeRects.map(r => r.left));
                let maxRight = Math.max(...nodeRects.map(r => r.right));

                const hasPremiumSelected = nodeRects.some(r => r.isPremium);

                if (!hasPremiumSelected) {
                    console.log('No premium vehicles selected, limiting to standard area');

                    const premiumDivider = document.querySelector('.premium-divider-v');
                    if (premiumDivider) {
                        const dividerRect = premiumDivider.getBoundingClientRect();
                        maxRight = Math.min(maxRight, dividerRect.left);
                        console.log('Limited screenshot width to exclude premium area');
                    }
                }

                const area = {
                    x: Math.max(0, minLeft - treeRect.left - 20), 
                    y: Math.max(0, minTop - treeRect.top - 50), 
                    width: Math.max(200, (maxRight - minLeft) + 40), 
                    height: Math.max(200, (maxBottom - minTop) + 100) 
                };

                console.log('Alternative screenshot area by selected nodes:', area);
                console.log('Premium vehicles included:', hasPremiumSelected);
                return area;
            }
        }

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

        const standardNodes = rankedNodes.filter(node => !node.isPremium);
        const nodesToUse = standardNodes.length > 0 ? standardNodes : rankedNodes;

        nodesToUse.sort((a, b) => a.top - b.top);

        console.log(`Found ${nodesToUse.length} nodes for fallback (${standardNodes.length} standard, ${rankedNodes.length - standardNodes.length} premium)`);

        const treeRect = treeGrid.getBoundingClientRect();
        const firstNode = nodesToUse[Math.floor(nodesToUse.length * 0.3)]; 
        const lastNode = nodesToUse[Math.floor(nodesToUse.length * 0.9)];  

        const allLefts = nodesToUse.map(n => n.rect.left);
        const allRights = nodesToUse.map(n => n.rect.right);

        const area = {
            x: Math.max(0, Math.min(...allLefts) - treeRect.left - 20),
            y: Math.max(0, firstNode.rect.top - treeRect.top - 50),
            width: Math.max(200, (Math.max(...allRights) - Math.min(...allLefts)) + 40),
            height: Math.max(200, (lastNode.rect.bottom - firstNode.rect.top) + 100)
        };

        return area;
    }, calculateScreenshotAreaByRanks: function (selectedRanks) {
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

        const { foundRanks, rankElementMap } = this.findRankElements();

        console.log(`Available ranks: [${foundRanks.join(', ')}]`);
        console.log(`Looking for ranks: ${minRank} to ${maxRank}`);

        let topElement = null;
        let bottomElement = null;

        if (rankElementMap.has(minRank)) {
            topElement = rankElementMap.get(minRank);
            console.log(`Found top element for rank ${minRank}`);
        }

        if (rankElementMap.has(maxRank)) {
            bottomElement = rankElementMap.get(maxRank);
            console.log(`Found bottom element for rank ${maxRank}`);
        }

        if (minRank === maxRank && topElement) {
            bottomElement = topElement;
            console.log(`Single rank ${minRank} - using same element for both boundaries`);
        }

        if (!topElement && bottomElement && foundRanks.length > 0) {
            const availableLowerRanks = foundRanks.filter(r => r <= maxRank).sort((a, b) => b - a);
            if (availableLowerRanks.length > 0) {
                const nearestLowerRank = availableLowerRanks[0];
                topElement = rankElementMap.get(nearestLowerRank);
                console.log(`Using nearest lower rank ${nearestLowerRank} as top element`);
            }
        }

        if (!bottomElement && topElement && foundRanks.length > 0) {
            const availableHigherRanks = foundRanks.filter(r => r >= minRank).sort((a, b) => a - b);
            if (availableHigherRanks.length > 0) {
                const nearestHigherRank = availableHigherRanks[0];
                bottomElement = rankElementMap.get(nearestHigherRank);
                console.log(`Using nearest higher rank ${nearestHigherRank} as bottom element`);
            }
        }

        if (!topElement || !bottomElement) {
            console.error(`CRITICAL: Could not find required rank elements!`);
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

        let width = treeRect.width;
        const premiumDivider = document.querySelector('.premium-divider-v');

        if (premiumDivider) {
            const dividerRect = premiumDivider.getBoundingClientRect();
            const dividerPosition = dividerRect.left - treeRect.left;
            width = Math.max(200, dividerPosition - 10);
            console.log('Limited screenshot width to exclude premium area:', width, 'divider at:', dividerPosition);
        } else {
            const standardNodes = document.querySelectorAll('.tree-grid-item.tech-cat-standard');
            const premiumNodes = document.querySelectorAll('.tree-grid-item.tech-cat-premium, .tree-grid-item.tech-cat-event');

            if (standardNodes.length > 0 && premiumNodes.length > 0) {
                const standardRights = Array.from(standardNodes).map(node => {
                    const rect = node.getBoundingClientRect();
                    return rect.right - treeRect.left;
                });
                const maxStandardRight = Math.max(...standardRights);
                width = maxStandardRight + 20;
                console.log('No divider found, calculated width by nodes:', width);
            }
        }

        const area = {
            x: 0,
            y: Math.max(0, topRect.top - treeRect.top - 10),
            width: Math.min(width, treeRect.width),
            height: Math.max(50, (bottomRect.bottom - topRect.top) + 20)
        };

        console.log(`SUCCESS: Calculated screenshot area for ranks ${minRank}-${maxRank}:`, area);
        return area;
    },
    calculateScreenshotArea: function (selectedRanks) {
        return this.calculateScreenshotAreaByRanks(selectedRanks);
    }, createScreenshot: async function (selectedRanks, filename, onProgress) {
        try {
            console.log('Starting screenshot creation for ranks:', selectedRanks);

            if (onProgress) onProgress('Подготовка области...');

            const treeContainer = document.getElementById('tech-tree-container-id');
            const summaryContainer = document.getElementById('summary-container');

            if (!treeContainer) {
                throw new Error('Tech tree container not found');
            }

            console.log('Tree container found:', treeContainer);
            console.log('Summary container found:', summaryContainer);

            const baseScreenshotArea = this.calculateScreenshotAreaByRanks(selectedRanks);
            if (!baseScreenshotArea) {
                throw new Error(`Не удалось найти ранги ${selectedRanks.join(', ')} на странице.`);
            }

            const { foundRanks } = this.findRankElements();

            if (foundRanks.length === 0) {
                throw new Error('No ranks found on the page');
            }

            const maxAvailableRank = Math.max(...foundRanks);
            const maxSelectedRank = Math.max(...selectedRanks);
            const treeGridRect = document.querySelector('.tree-grid').getBoundingClientRect();

            const summaryRect = summaryContainer ? summaryContainer.getBoundingClientRect() : null;
            const viewportHeight = window.innerHeight;

            let distanceToSummary = Infinity;
            if (summaryRect) {
                const areaBottomInViewport = baseScreenshotArea.y + baseScreenshotArea.height - treeGridRect.top + treeGridRect.top;
                distanceToSummary = Math.abs(summaryRect.top - areaBottomInViewport);
            }

            const isMaxAvailableRank = maxSelectedRank >= maxAvailableRank;
            const isCloseToSummaryPanel = distanceToSummary < viewportHeight * 0.4;

            const useOldMethod = isMaxAvailableRank || isCloseToSummaryPanel;

            console.log('Panel positioning strategy:', {
                foundRanks: foundRanks,
                maxAvailableRank: maxAvailableRank,
                maxSelectedRank: maxSelectedRank,
                isMaxAvailableRank: isMaxAvailableRank,
                distanceToSummary: distanceToSummary,
                viewportHeight: viewportHeight,
                isCloseToSummaryPanel: isCloseToSummaryPanel,
                useOldMethod: useOldMethod ? 'OLD (extend area)' : 'NEW (relocate panel)'
            });

            const tempContainer = document.createElement('div');
            tempContainer.style.position = 'absolute';
            tempContainer.style.top = '-10000px';
            tempContainer.style.left = '-10000px';
            tempContainer.style.background = '#1a1a1a';
            tempContainer.style.width = treeContainer.scrollWidth + 'px';
            tempContainer.style.overflow = 'visible';

            const treeClone = treeContainer.cloneNode(true);
            treeClone.style.paddingBottom = '10px';
            treeClone.style.position = 'relative';
            tempContainer.appendChild(treeClone);

            let finalScreenshotArea;

            if (useOldMethod) {

                if (summaryContainer) {
                    const summaryClone = summaryContainer.cloneNode(true);

                    const cloneNamesContainer = summaryClone.querySelector('#selected-names-container');
                    const cloneButtons = summaryClone.querySelectorAll('button');

                    if (cloneNamesContainer) {
                        cloneNamesContainer.remove();
                    }
                    cloneButtons.forEach(btn => btn.remove());

                    summaryClone.style.position = 'relative';
                    summaryClone.style.background = 'rgba(40, 40, 40, 0.95)';
                    summaryClone.style.backdropFilter = 'blur(3px)';
                    summaryClone.style.borderTop = '1px solid #666';
                    summaryClone.style.marginTop = '20px'; 
                    summaryClone.style.padding = '12px 20px';
                    summaryClone.style.display = 'flex';
                    summaryClone.style.flexDirection = 'column';
                    summaryClone.style.alignItems = 'center';
                    summaryClone.style.gap = '8px';
                    summaryClone.style.width = treeContainer.scrollWidth + 'px';

                    tempContainer.appendChild(summaryClone);
                    console.log('Added summary panel using OLD method (at bottom)');
                }

                const realSummaryHeight = summaryContainer ? summaryContainer.getBoundingClientRect().height : 80;
                const additionalPadding = 50; 
                const panelHeight = realSummaryHeight + additionalPadding;

                finalScreenshotArea = {
                    x: baseScreenshotArea.x,
                    y: baseScreenshotArea.y,
                    width: baseScreenshotArea.width,
                    height: baseScreenshotArea.height + panelHeight
                };


            } else {

                if (summaryContainer) {
                    const summaryClone = summaryContainer.cloneNode(true);

                    const cloneNamesContainer = summaryClone.querySelector('#selected-names-container');
                    const cloneButtons = summaryClone.querySelectorAll('button');

                    if (cloneNamesContainer) {
                        cloneNamesContainer.remove();
                    }
                    cloneButtons.forEach(btn => btn.remove());

                    summaryClone.style.position = 'absolute';
                    summaryClone.style.background = 'rgba(40, 40, 40, 0.95)';
                    summaryClone.style.backdropFilter = 'blur(3px)';
                    summaryClone.style.border = '1px solid #666';
                    summaryClone.style.borderRadius = '8px';
                    summaryClone.style.padding = '12px 20px';
                    summaryClone.style.display = 'flex';
                    summaryClone.style.flexDirection = 'column';
                    summaryClone.style.alignItems = 'center';
                    summaryClone.style.gap = '8px';
                    summaryClone.style.zIndex = '1000';
                    summaryClone.style.boxShadow = '0 4px 12px rgba(0,0,0,0.5)';

                    const panelWidth = 300;
                    const panelHeight = 80;
                    const margin = 20;

                    const panelX = Math.max(baseScreenshotArea.x + baseScreenshotArea.width - panelWidth - margin, baseScreenshotArea.x + margin);
                    const panelY = baseScreenshotArea.y + baseScreenshotArea.height + margin;

                    summaryClone.style.left = panelX + 'px';
                    summaryClone.style.top = panelY + 'px';
                    summaryClone.style.width = panelWidth + 'px';

                    treeClone.appendChild(summaryClone);
                    console.log('Added relocated summary panel to tree clone');
                }

                const panelHeight = 100;
                const panelMargin = 30;
                finalScreenshotArea = {
                    x: baseScreenshotArea.x,
                    y: baseScreenshotArea.y,
                    width: baseScreenshotArea.width,
                    height: baseScreenshotArea.height + panelHeight + panelMargin
                };
            }

            document.body.appendChild(tempContainer);
            console.log('Temp container added to document');

            await new Promise(resolve => setTimeout(resolve, 100));

            if (onProgress) onProgress('Создание скриншота...');

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

            if (tempContainer && tempContainer.parentNode) {
                document.body.removeChild(tempContainer);
                console.log('Temp container removed');
            }

            if (onProgress) onProgress('Обработка изображения...');

            console.log('Screenshot area to crop:', finalScreenshotArea);

            const maxWidth = 4000;
            const maxHeight = 6000;

            if (finalScreenshotArea.width > maxWidth || finalScreenshotArea.height > maxHeight) {
                console.warn(`Screenshot area too large, limiting size`);
                finalScreenshotArea.width = Math.min(finalScreenshotArea.width, maxWidth);
                finalScreenshotArea.height = Math.min(finalScreenshotArea.height, maxHeight);
            }

            const croppedCanvas = document.createElement('canvas');
            const ctx = croppedCanvas.getContext('2d');

            const scale = 3;
            const cropX = Math.max(0, finalScreenshotArea.x * scale);
            const cropY = Math.max(0, finalScreenshotArea.y * scale);
            const cropWidth = Math.min(canvas.width - cropX, finalScreenshotArea.width * scale);
            const cropHeight = Math.min(canvas.height - cropY, finalScreenshotArea.height * scale);

            const maxCanvasSize = 16384;
            if (cropWidth > maxCanvasSize || cropHeight > maxCanvasSize) {
                console.warn(`Final canvas size too large, reducing scale`);
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
                finalCanvasSize: {
                    width: croppedCanvas.width,
                    height: croppedCanvas.height
                }
            });

            if (croppedCanvas.width < cropWidth || croppedCanvas.height < cropHeight) {
                ctx.drawImage(
                    canvas,
                    cropX, cropY, cropWidth, cropHeight,
                    0, 0, croppedCanvas.width, croppedCanvas.height
                );
                console.log('Screenshot scaled down due to size limitations');
            } else {
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

            try {
                if (tempContainer && tempContainer.parentNode) {
                    document.body.removeChild(tempContainer);
                    console.log('Temp container removed in error handler');
                }
            } catch (cleanupError) {
                console.log('Temp container cleanup failed or already removed');
            }

            throw error;
        }
    },
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
                    if (navigator.clipboard && navigator.clipboard.write) {
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

    generateScreenshotFilename: function () {
        try {
            const url = new URL(window.location.href);
            const pathParts = url.pathname.split('/').filter(p => p);

            let filename = 'screenshot';
            if (pathParts.length >= 3 && pathParts[0] === 'tree') {
                const nation = pathParts[1];
                const vehicleType = pathParts[2];
                filename = `tree-${nation}-${vehicleType}`;
            }

            const params = url.searchParams;
            const selected = params.get('selected');
            if (selected) {
                filename += '-selected';
            }

            const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
            filename += `-${timestamp}.png`;

            return filename;
        } catch (error) {
            console.error('Error generating filename:', error);
            return `screenshot-${Date.now()}.png`;
        }
    },
    showRpLimitWarning: function (message) {
        const existingWarnings = document.querySelectorAll('.rp-limit-toast');
        existingWarnings.forEach(w => w.remove());

        const toast = document.createElement('div');
        toast.className = 'rp-limit-toast';
        toast.innerHTML = `
        <i class="bi bi-exclamation-triangle"></i>
        <span>${message}</span>
    `;

        toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 1050;
        background-color: #856404;
        color: #fff3cd;
        border: 1px solid #ffeaa7;
        border-radius: 8px;
        padding: 12px 20px;
        display: flex;
        align-items: center;
        gap: 10px;
        font-size: 0.9em;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
        animation: slideInRight 0.3s ease-out, fadeOut 0.3s ease-in 2.7s forwards;
        max-width: 350px;
        word-wrap: break-word;
    `;

        if (!document.querySelector('#rp-limit-animations')) {
            const style = document.createElement('style');
            style.id = 'rp-limit-animations';
            style.textContent = `
            @keyframes slideInRight {
                from {
                    transform: translateX(100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }
            
            @keyframes fadeOut {
                from {
                    opacity: 1;
                }
                to {
                    opacity: 0;
                    transform: translateX(100%);
                }
            }
        `;
            document.head.appendChild(style);
        }

        document.body.appendChild(toast);

        setTimeout(() => {
            if (toast && toast.parentNode) {
                toast.remove();
            }
        }, 3000);

        console.log('RP limit warning shown:', message);
    },

    updateNodesLimitState: function (maxRpLimit, currentRp) {
        const vehicleNodes = document.querySelectorAll('.node-content.vehicle-node');

        vehicleNodes.forEach(node => {
            const container = node.closest('.tree-grid-item');

            if (maxRpLimit !== null && currentRp >= maxRpLimit) {
                node.classList.add('limit-blocked');
                if (container) container.classList.add('limit-blocked');
            } else {
                node.classList.remove('limit-blocked');
                if (container) container.classList.remove('limit-blocked');
            }
        });
    }
};