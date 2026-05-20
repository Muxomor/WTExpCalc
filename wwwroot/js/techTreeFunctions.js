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
    _publicApiMethods: Object.freeze([
        'positionFolderPopup',
        'updateNodesLimitState',
        'showRpLimitWarning',
        'drawConnections',
        'copyTextToClipboard_fallback',
        'copyScreenshotToClipboard',
        'downloadScreenshot',
        'generateScreenshotFilename',
        'downloadScreenshot4K',
        'generateScreenshotFilename4K'
    ]),
    _invokeCallback: function (callbackRef, methodName, ...args) {
        if (!callbackRef) return;
        try {
            callbackRef.invokeMethodAsync(methodName, ...args);
        } catch (error) {
            console.error(`Error invoking callback ${methodName}:`, error);
        }
    },
    _reportProgress: function (onProgress, message) {
        if (onProgress) onProgress(message);
    },
    _removeElement: function (element) {
        if (element && element.parentNode) {
            element.parentNode.removeChild(element);
        }
    },
    _createTempContainer: function (treeContainer) {
        const tempContainer = document.createElement('div');
        tempContainer.style.position = 'absolute';
        tempContainer.style.top = '-10000px';
        tempContainer.style.left = '-10000px';
        tempContainer.style.background = '#1a1a1a';
        tempContainer.style.width = treeContainer.scrollWidth + 'px';
        tempContainer.style.overflow = 'visible';
        return tempContainer;
    },
    _createTreeClone: function (treeContainer) {
        const treeClone = treeContainer.cloneNode(true);
        treeClone.style.paddingBottom = '10px';
        treeClone.style.position = 'relative';
        return treeClone;
    },
    _createSummaryClone: function (summaryContainer) {
        if (!summaryContainer) return null;
        const summaryClone = summaryContainer.cloneNode(true);
        const cloneNamesContainer = summaryClone.querySelector('#selected-names-container');
        const cloneButtons = summaryClone.querySelectorAll('button');
        if (cloneNamesContainer) cloneNamesContainer.remove();
        cloneButtons.forEach(btn => btn.remove());

        summaryClone.style.background = 'rgba(40, 40, 40, 0.95)';
        summaryClone.style.backdropFilter = 'blur(3px)';
        summaryClone.style.borderTop = '1px solid #666';
        summaryClone.style.padding = '12px 20px';
        summaryClone.style.display = 'flex';
        summaryClone.style.flexDirection = 'column';
        summaryClone.style.alignItems = 'center';
        summaryClone.style.gap = '8px';
        return summaryClone;
    },
    _getSummaryPanelHeight: function (summaryContainer) {
        const summaryPanelElement = summaryContainer ? summaryContainer.querySelector('#summary-panel') : null;
        return summaryPanelElement ? summaryPanelElement.getBoundingClientRect().height + 44 : 80;
    },
    _buildConnectionDataFromDom: function () {
        const svg = document.querySelector('.tree-connections-overlay');
        if (!svg) return [];

        const lines = svg.querySelectorAll('line[id^="dep-"]');
        const connectionData = [];

        lines.forEach(line => {
            const match = /^dep-(\d+)-(\d+)$/.exec(line.id);
            if (!match) return;

            const targetNodeId = match[1];
            const sourceNodeId = match[2];

            connectionData.push({
                lineId: line.id,
                sourceElementId: `node-${sourceNodeId}`,
                targetElementId: `node-${targetNodeId}`
            });
        });

        return connectionData;
    },
    _refreshConnectionsBeforeScreenshot: async function () {
        const connectionData = this._buildConnectionDataFromDom();
        if (!connectionData.length) return;

        this.drawConnections(connectionData);
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    },
    _resolveScreenshotContext: function (selectedRanks) {
        const treeContainer = document.getElementById('tech-tree-container-id');
        const summaryContainer = document.getElementById('summary-container');
        if (!treeContainer) {
            throw new Error('Tech tree container not found');
        }

        const baseScreenshotArea = this.calculateScreenshotAreaByRanks(selectedRanks);
        if (!baseScreenshotArea) {
            throw new Error(`Не удалось найти ранги ${selectedRanks.join(', ')} на странице.`);
        }

        const { foundRanks } = this.findRankElements();
        if (!foundRanks || foundRanks.length === 0) {
            throw new Error('No ranks found on the page');
        }

        const maxAvailableRank = Math.max(...foundRanks);
        const maxSelectedRank = Math.max(...selectedRanks);
        return {
            treeContainer,
            summaryContainer,
            baseScreenshotArea,
            useOldMethod: maxSelectedRank >= maxAvailableRank
        };
    },
    _buildScreenshotScene: function (treeContainer, summaryContainer, baseScreenshotArea, useOldMethod) {
        const tempContainer = this._createTempContainer(treeContainer);
        const treeClone = this._createTreeClone(treeContainer);
        tempContainer.appendChild(treeClone);

        const summaryClone = this._createSummaryClone(summaryContainer);
        if (summaryClone) {
            if (useOldMethod) {
                summaryClone.style.position = 'relative';
                summaryClone.style.marginTop = '20px';
                summaryClone.style.width = treeContainer.scrollWidth + 'px';
                tempContainer.appendChild(summaryClone);
            } else {
                summaryClone.style.position = 'absolute';
                summaryClone.style.zIndex = '1000';
                summaryClone.style.left = baseScreenshotArea.x + 'px';
                summaryClone.style.top = (baseScreenshotArea.y + baseScreenshotArea.height + 20) + 'px';
                summaryClone.style.width = baseScreenshotArea.width + 'px';
                treeClone.appendChild(summaryClone);
            }
        }

        const panelHeight = this._getSummaryPanelHeight(summaryContainer);
        const finalScreenshotArea = {
            x: baseScreenshotArea.x,
            y: baseScreenshotArea.y,
            width: baseScreenshotArea.width,
            height: baseScreenshotArea.height + panelHeight
        };

        return { tempContainer, finalScreenshotArea };
    },
    _renderScreenshotCanvas: async function (tempContainer, screenshotArea, scale) {
        document.body.appendChild(tempContainer);
        await new Promise(resolve => setTimeout(resolve, 100));

        return html2canvas(tempContainer, {
            scale: scale,
            useCORS: true,
            allowTaint: true,
            backgroundColor: '#1a1a1a',
            logging: false,
            x: screenshotArea.x,
            y: screenshotArea.y,
            width: screenshotArea.width,
            height: screenshotArea.height,
            windowWidth: tempContainer.scrollWidth,
            windowHeight: tempContainer.scrollHeight,
            scrollX: tempContainer.scrollLeft,
            scrollY: tempContainer.scrollTop
        });
    },
    _limitCanvasSize: function (canvas, maxSize, modeName) {
        if (canvas.width <= maxSize && canvas.height <= maxSize) {
            return canvas;
        }

        const scaleDown = Math.min(maxSize / canvas.width, maxSize / canvas.height);
        const finalWidth = Math.floor(canvas.width * scaleDown);
        const finalHeight = Math.floor(canvas.height * scaleDown);

        const scaledCanvas = document.createElement('canvas');
        scaledCanvas.width = finalWidth;
        scaledCanvas.height = finalHeight;
        const ctx = scaledCanvas.getContext('2d');
        ctx.drawImage(canvas, 0, 0, finalWidth, finalHeight);
        console.warn(`${modeName} canvas was downscaled from ${canvas.width}x${canvas.height} to ${finalWidth}x${finalHeight}`);
        return scaledCanvas;
    },
    _createScreenshotInternal: async function (selectedRanks, options, onProgress) {
        let tempContainer = null;
        try {
            this._reportProgress(onProgress, options.preparingMessage);
            // Recalculate arrows once right before screenshot to avoid stale positions after zoom.
            await this._refreshConnectionsBeforeScreenshot();
            const context = this._resolveScreenshotContext(selectedRanks);
            const scene = this._buildScreenshotScene(
                context.treeContainer,
                context.summaryContainer,
                context.baseScreenshotArea,
                context.useOldMethod
            );
            tempContainer = scene.tempContainer;

            this._reportProgress(onProgress, options.renderMessage);
            const canvas = await this._renderScreenshotCanvas(tempContainer, scene.finalScreenshotArea, options.scale);

            this._reportProgress(onProgress, options.processMessage);
            return this._limitCanvasSize(canvas, options.maxCanvasSize, options.modeName);
        } finally {
            this._removeElement(tempContainer);
        }
    },
    _canvasToBlob: function (canvas) {
        return new Promise((resolve, reject) => {
            canvas.toBlob((blob) => {
                if (!blob) {
                    reject(new Error('Canvas blob creation failed'));
                    return;
                }
                resolve(blob);
            }, 'image/png');
        });
    },
    _downloadBlob: function (blob, filename) {
        const url = URL.createObjectURL(blob);
        try {
            const link = document.createElement('a');
            link.download = filename;
            link.href = url;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } finally {
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    },
    createScreenshot4K: async function (selectedRanks, filename, onProgress) {
        try {
            return await this._createScreenshotInternal(selectedRanks, {
                preparingMessage: 'Подготовка 4K области...',
                renderMessage: 'Создание 4K скриншота...',
                processMessage: 'Обработка 4K изображения...',
                scale: 4,
                // "4K" in this project means 4000x4000 limit mode, not UHD 3840x2160.
                maxCanvasSize: 4000,
                modeName: '4K (4000x4000 limit mode)'
            }, onProgress);
        } catch (error) {
            console.error('Error creating 4K screenshot:', error);
            throw error;
        }
    },

    downloadScreenshot4K: async function (selectedRanks, filename, progressCallbackRef, completeCallbackRef) {
        try {
            const canvas = await this.createScreenshot4K(selectedRanks, filename, (message) => {
                this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', message);
            });

            this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', 'Подготовка 4K загрузки...');
            const blob = await this._canvasToBlob(canvas);
            this._downloadBlob(blob, filename || 'screenshot-4k.png');
            this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', true, '4K скриншот сохранен');
        } catch (error) {
            console.error('Error in downloadScreenshot4K:', error);
            this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', false, 'Ошибка создания 4K скриншота: ' + error.message);
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

                    if (foundNumber > 9) {
                        const firstDigit = parseInt(foundNumber.toString()[0]);
                        if (firstDigit >= 1 && firstDigit <= 9) {
                            rank = firstDigit;
                            console.log(`Extracted rank ${rank} from compound number ${foundNumber} in: "${rankText}"`);
                            break;
                        }
                    } else if (foundNumber >= 1 && foundNumber <= 9) {
                        rank = foundNumber;
                        console.log(`Found direct rank ${rank} in: "${rankText}"`);
                        break;
                    }
                }
            }

            if (rank && rank >= 1 && rank <= 9) {
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
    },
    createScreenshot: async function (selectedRanks, filename, onProgress) {
        try {
            return await this._createScreenshotInternal(selectedRanks, {
                preparingMessage: 'Подготовка области...',
                renderMessage: 'Создание скриншота...',
                processMessage: 'Обработка изображения...',
                scale: 3,
                maxCanvasSize: 16384,
                modeName: 'Standard screenshot'
            }, onProgress);
        } catch (error) {
            console.error('Error creating screenshot:', error);
            throw error;
        }
    },
    copyScreenshotToClipboard: async function (selectedRanks, progressCallbackRef, completeCallbackRef) {
        try {
            const canvas = await this.createScreenshot(selectedRanks, null, (message) => {
                this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', message);
            });

            this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', 'Копирование в буфер обмена...');
            const blob = await this._canvasToBlob(canvas);
            if (navigator.clipboard && navigator.clipboard.write && typeof ClipboardItem !== 'undefined') {
                await navigator.clipboard.write([
                    new ClipboardItem({
                        'image/png': blob
                    })
                ]);
                this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', true, 'Скриншот скопирован в буфер обмена');
            } else {
                this._downloadBlob(blob, this.generateScreenshotFilename());
                this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', true, 'Скриншот сохранен (буфер обмена недоступен на HTTP)');
            }
        } catch (error) {
            console.error('Error in copyScreenshotToClipboard:', error);
            this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', false, 'Ошибка создания скриншота: ' + error.message);
        }
    },

    downloadScreenshot: async function (selectedRanks, filename, progressCallbackRef, completeCallbackRef) {
        try {
            const canvas = await this.createScreenshot(selectedRanks, filename, (message) => {
                this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', message);
            });

            this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', 'Подготовка загрузки...');
            const blob = await this._canvasToBlob(canvas);
            this._downloadBlob(blob, filename || 'screenshot.png');
            this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', true, 'Скриншот сохранен');
        } catch (error) {
            console.error('Error in downloadScreenshot:', error);
            this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', false, 'Ошибка создания скриншота: ' + error.message);
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