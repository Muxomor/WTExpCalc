window.techTreeFunctions = {
    setDocumentLanguage: function (language) {
        document.documentElement.lang = language === 'en' ? 'en' : 'ru';
    },

    ensureCalculatorFunctions: function () {
        if (window.calculatorFunctions) return Promise.resolve();
        if (this._calculatorFunctionsPromise) return this._calculatorFunctionsPromise;

        var self = this;
        this._calculatorFunctionsPromise = new Promise(function (resolve, reject) {
            var script = document.createElement('script');
            script.src = new URL('js/calculatorFunctions.js', document.baseURI).href;
            script.async = true;
            script.onload = function () { resolve(); };
            script.onerror = function () {
                self._calculatorFunctionsPromise = null;
                reject(new Error('Failed to load calculator functions'));
            };
            document.head.appendChild(script);
        });

        return this._calculatorFunctionsPromise;
    },

    rememberFocus: function () {
        this._previousFocus = document.activeElement;
    },

    restoreFocus: function () {
        const previous = this._previousFocus;
        this._previousFocus = null;
        if (previous && previous.isConnected && typeof previous.focus === 'function') {
            previous.focus();
        }
    },

    /* Load html2canvas only when the user actually requests a screenshot. */
    ensureHtml2Canvas: function () {
        if (window.html2canvas) return Promise.resolve();
        if (this._html2CanvasPromise) return this._html2CanvasPromise;

        var self = this;
        this._html2CanvasPromise = new Promise(function (resolve, reject) {
            var script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js';
            script.async = true;
            script.onload = function () { resolve(); };
            script.onerror = function () {
                self._html2CanvasPromise = null;
                reject(new Error('Failed to load html2canvas'));
            };
            document.head.appendChild(script);
        });

        return this._html2CanvasPromise;
    },

    /* Waits until after the next two animation frames — use after Blazor DOM updates before measuring boxes. */
    waitTwoAnimationFramesAsync: function () {
        return new Promise(function (resolve) {
            requestAnimationFrame(function () {
                requestAnimationFrame(function () { resolve(true); });
            });
        });
    },

    disconnectTechTreeLayout: function () {
        if (!this._techTreeLayoutTeardown) return;
        try {
            this._techTreeLayoutTeardown();
        } finally {
            this._techTreeLayoutTeardown = null;
        }
    },

    /* Re-draw dependency lines after window resize / layout changes. Debounced. */
    observeTechTreeLayout: function (containerId, dotnetRef, debounceMs) {
        this.disconnectTechTreeLayout();
        if (!dotnetRef || !containerId) return;

        var el = document.getElementById(containerId);
        if (!el) return;

        var self = this;
        var delay = typeof debounceMs === 'number' ? debounceMs : 110;
        var timer = null;
        function scheduleRedraw() {
            window.clearTimeout(timer);
            timer = window.setTimeout(function () {
                timer = null;
                self.syncSummaryPosition();
                dotnetRef.invokeMethodAsync('RedrawConnectionsDebouncedAsync').catch(function () {});
            }, delay);
        }

        this.syncSummaryPosition();

        var ro = null;
        try {
            if (typeof ResizeObserver !== 'undefined') {
                ro = new ResizeObserver(function () { scheduleRedraw(); });
                ro.observe(el);
            }
        } catch (e) { /* ResizeObserver unsupported */ }

        window.addEventListener('resize', scheduleRedraw);
        var vv = window.visualViewport;
        if (vv) vv.addEventListener('resize', scheduleRedraw);

        this._techTreeLayoutTeardown = function () {
            window.clearTimeout(timer);
            timer = null;
            if (ro) {
                try { ro.disconnect(); } catch (e) { /* ignore */ }
            }
            window.removeEventListener('resize', scheduleRedraw);
            if (vv) vv.removeEventListener('resize', scheduleRedraw);
        };
    },

    /* Keep the fixed summary bar under the tree column and centered on it. */
    syncSummaryPosition: function () {
        const summary = document.getElementById('summary-container');
        const container = document.getElementById('tech-tree-container-id');
        if (!summary || !container) return;

        const rect = container.getBoundingClientRect();
        const viewportWidth = document.documentElement.clientWidth;
        let width = rect.width;
        let left = rect.left;
        const minWidth = 360;

        if (width < minWidth) {
            const center = left + width / 2;
            width = Math.min(viewportWidth, minWidth);
            left = Math.max(0, Math.min(center - width / 2, viewportWidth - width));
        }

        summary.style.left = left + 'px';
        summary.style.width = width + 'px';
        summary.style.right = 'auto';
    },

    positionFolderPopup: function (popupId, targetNodeId) {
        var popupElement = document.getElementById(popupId);
        var targetNodeElement = document.getElementById(targetNodeId);

        if (!popupElement || !targetNodeElement) {
            if (popupElement) popupElement.style.visibility = 'hidden';
            return;
        }

        popupElement.style.visibility = 'hidden';
        popupElement.style.top = '-9999px';
        popupElement.style.left = '-9999px';
        popupElement.style.display = 'block';

        var popupHeight = popupElement.offsetHeight;
        var popupWidth = popupElement.offsetWidth;

        var targetRect = targetNodeElement.getBoundingClientRect();
        var summaryPanel = document.getElementById('summary-container');
        var summaryPanelHeight = summaryPanel ? summaryPanel.offsetHeight : 0;

        var viewportHeight = window.innerHeight;
        var viewportWidth = window.innerWidth;
        var spaceBelow = viewportHeight - targetRect.bottom - summaryPanelHeight - 5;
        var spaceAbove = targetRect.top - 5;

        var top;
        var left = targetRect.left;

        if (popupHeight <= spaceBelow) {
            top = targetRect.bottom + 5;
        } else if (popupHeight <= spaceAbove) {
            top = targetRect.top - popupHeight - 5;
        } else {
            top = targetRect.bottom + 5;
        }

        // Keep the fixed popup inside the viewport. Fixed positioning prevents
        // it from contributing to the scrollable area of the tree container.
        left = Math.max(10, Math.min(left, viewportWidth - popupWidth - 10));
        top = Math.max(10, Math.min(top, viewportHeight - popupHeight - 10));

        popupElement.style.left = left + 'px';
        popupElement.style.top = top + 'px';
        popupElement.style.visibility = 'visible';
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
            } else {
                console.error('document.execCommand("copy") failed.');
                alert("Не удалось скопировать текст. Браузер не поддерживает или заблокировал команду.");
            }
        } catch (err) {
            console.error('Exception while copying text: ', err);
            alert("Ошибка при копировании текста (fallback).");
        }
    },
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

        // The live bar is positioned by JS; reset those inline values so the
        // screenshot clone keeps using its own crop coordinates.
        summaryClone.style.left = '';
        summaryClone.style.right = '';
        summaryClone.style.width = '';

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

        const paths = svg.querySelectorAll('path[id^="dep-"]');
        const connectionData = [];

        paths.forEach(path => {
            const match = /^dep-(\d+)-(\d+)$/.exec(path.id);
            if (!match) return;

            const targetNodeId = match[1];
            const sourceNodeId = match[2];

            connectionData.push({
                lineId: path.id,
                sourceElementId: `node-${sourceNodeId}`,
                targetElementId: `node-${targetNodeId}`
            });
        });

        return connectionData;
    },

    _buildConnectionPath: function (x1, y1, x2, y2) {
        var colThreshold = 12;
        var rankBandThreshold = 50;
        var dx = x2 - x1;
        var dy = y2 - y1;

        if (Math.abs(dx) < colThreshold) {
            return 'M ' + x1 + ' ' + y1 + ' L ' + x2 + ' ' + y2;
        }

        if (Math.abs(dy) < rankBandThreshold) {
            var routeY = y1 + Math.min(18, Math.max(8, Math.abs(dy) * 0.5 + 8));
            return 'M ' + x1 + ' ' + y1 + ' L ' + x1 + ' ' + routeY + ' L ' + x2 + ' ' + routeY + ' L ' + x2 + ' ' + y2;
        }

        if (dy > 0) {
            // Cross-rank: vertical down, horizontal at target rank, then into target (wiki-style elbow)
            var elbowY = y2 - 12;
            if (elbowY <= y1 + 16) {
                elbowY = y1 + Math.max(24, dy * 0.45);
            }
            return 'M ' + x1 + ' ' + y1 + ' L ' + x1 + ' ' + elbowY + ' L ' + x2 + ' ' + elbowY + ' L ' + x2 + ' ' + y2;
        }

        var fallbackY = (y1 + y2) / 2;
        return 'M ' + x1 + ' ' + y1 + ' L ' + x1 + ' ' + fallbackY + ' L ' + x2 + ' ' + fallbackY + ' L ' + x2 + ' ' + y2;
    },
    _refreshConnectionsBeforeScreenshot: async function () {
        const connectionData = this._buildConnectionDataFromDom();
        if (!connectionData.length) return;

        this.drawConnections(connectionData);
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    },
    _resolveScreenshotContext: function (selectedRanks, includePremium) {
        const treeContainer = document.getElementById('tech-tree-container-id');
        const summaryContainer = document.getElementById('summary-container');
        if (!treeContainer) {
            throw new Error('Tech tree container not found');
        }

        const baseScreenshotArea = this.calculateScreenshotAreaByRanks(selectedRanks, includePremium);
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
    _buildScreenshotScene: function (treeContainer, summaryContainer, baseScreenshotArea, useOldMethod, includeSummary) {
        const tempContainer = this._createTempContainer(treeContainer);
        const treeClone = this._createTreeClone(treeContainer);
        tempContainer.appendChild(treeClone);

        if (includeSummary) {
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
        }

        const panelHeight = includeSummary ? this._getSummaryPanelHeight(summaryContainer) : 0;
        const finalScreenshotArea = {
            x: baseScreenshotArea.x,
            y: baseScreenshotArea.y,
            width: baseScreenshotArea.width,
            height: baseScreenshotArea.height + panelHeight
        };

        return { tempContainer, finalScreenshotArea };
    },
    _renderScreenshotCanvas: async function (tempContainer, screenshotArea, scale, maxCanvasSize) {
        await this.ensureHtml2Canvas();
        document.body.appendChild(tempContainer);
        await new Promise(resolve => setTimeout(resolve, 100));

        const maxDimension = Math.max(screenshotArea.width, screenshotArea.height);
        const targetLimit = (typeof maxCanvasSize === 'number' && maxCanvasSize > 0) ? maxCanvasSize : 16384;
        const effectiveScale = Math.min(scale, 16384 / maxDimension, targetLimit / maxDimension);

        return html2canvas(tempContainer, {
            scale: effectiveScale,
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
            const context = this._resolveScreenshotContext(selectedRanks, options.includePremium);
            const scene = this._buildScreenshotScene(
                context.treeContainer,
                context.summaryContainer,
                context.baseScreenshotArea,
                context.useOldMethod,
                options.includeSummary !== false
            );
            tempContainer = scene.tempContainer;

            this._reportProgress(onProgress, options.renderMessage);
            const canvas = await this._renderScreenshotCanvas(tempContainer, scene.finalScreenshotArea, options.scale, options.maxCanvasSize);

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
    _deliverScreenshotBlob: async function (blob, fallbackFilename, progressCallbackRef, completeCallbackRef, messages) {
        const copyProgress = messages.copyProgress || '';
        this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', copyProgress);

        if (navigator.clipboard && navigator.clipboard.write && typeof ClipboardItem !== 'undefined') {
            try {
                await navigator.clipboard.write([
                    new ClipboardItem({
                        'image/png': blob
                    })
                ]);
                this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', true, messages.clipboardSuccess);
                return;
            } catch (clipboardError) {
                console.warn('Clipboard write failed, falling back to download:', clipboardError);
            }
        }

        if (messages.downloadProgress) {
            this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', messages.downloadProgress);
        }
        this._downloadBlob(blob, fallbackFilename);
        this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', true, messages.downloadFallback);
    },
    createScreenshot: async function (selectedRanks, filename, onProgress, messages, includeSummary, includePremium) {
        try {
            messages = messages || {};
            return await this._createScreenshotInternal(selectedRanks, {
                preparingMessage: messages.preparing || '',
                renderMessage: messages.rendering || '',
                processMessage: messages.processing || '',
                scale: 4,
                // Keep the output within the 4000x4000 limit used by external platforms.
                maxCanvasSize: 4000,
                modeName: 'Screenshot',
                includeSummary: includeSummary !== false,
                includePremium: includePremium === true
            }, onProgress);
        } catch (error) {
            console.error('Error creating screenshot:', error);
            throw error;
        }
    },

    downloadScreenshot: async function (selectedRanks, filename, includeSummary, includePremium, messages, progressCallbackRef, completeCallbackRef) {
        try {
            messages = messages || {};
            const canvas = await this.createScreenshot(selectedRanks, filename, (message) => {
                this._invokeCallback(progressCallbackRef, 'OnScreenshotProgress', message);
            }, messages, includeSummary, includePremium);

            const blob = await this._canvasToBlob(canvas);
            await this._deliverScreenshotBlob(
                blob,
                filename || this.generateScreenshotFilename(),
                progressCallbackRef,
                completeCallbackRef,
                {
                    copyProgress: messages.copying || '',
                    downloadProgress: messages.downloadProgress || '',
                    clipboardSuccess: messages.clipboardSuccess || '',
                    downloadFallback: messages.downloadFallback || ''
                }
            );
        } catch (error) {
            console.error('Error in downloadScreenshot:', error);
            this._invokeCallback(completeCallbackRef, 'OnScreenshotComplete', false, (messages.error || '') + ': ' + error.message);
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
    drawConnections: function (connectionData) {
        if (!connectionData || connectionData.length === 0) return;

        var svg = document.querySelector('.tree-connections-overlay');
        if (!svg) return;
        var svgRect = svg.getBoundingClientRect();

        connectionData.forEach(function (conn) {
            var path = svg.querySelector('#' + conn.lineId);
            var sourceElem = document.getElementById(conn.sourceElementId);
            var targetElem = document.getElementById(conn.targetElementId);

            var isSourceVisible = sourceElem && sourceElem.offsetParent !== null;
            var isTargetVisible = targetElem && targetElem.offsetParent !== null;

            if (path && sourceElem && targetElem && isSourceVisible && isTargetVisible) {
                try {
                    var sourceRect = sourceElem.getBoundingClientRect();
                    var targetRect = targetElem.getBoundingClientRect();

                    var x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left;
                    var y1 = sourceRect.bottom - svgRect.top;
                    var x2 = targetRect.left + targetRect.width / 2 - svgRect.left;
                    var y2 = targetRect.top - svgRect.top;

                    path.setAttribute('d', window.techTreeFunctions._buildConnectionPath(x1, y1, x2, y2));
                    path.style.visibility = 'visible';
                } catch (e) {
                    if (path) path.style.visibility = 'hidden';
                }
            } else {
                if (path) path.style.visibility = 'hidden';
            }
        });
    },
    findRankElements: function () {
        const allRankLabels = document.querySelectorAll('.rank-label');
        const foundRanks = [];
        const rankElementMap = new Map();

        allRankLabels.forEach(label => {
            const rank = Number.parseInt(label.getAttribute('data-rank'), 10);

            if (Number.isInteger(rank) && rank >= 1 && rank <= 9 && !rankElementMap.has(rank)) {
                foundRanks.push(rank);
                rankElementMap.set(rank, label);
            }
        });

        foundRanks.sort((a, b) => a - b);
        return { foundRanks, rankElementMap };
    },
    calculateScreenshotAreaByRanks: function (selectedRanks, includePremium) {
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


        let topElement = null;
        let bottomElement = null;

        if (rankElementMap.has(minRank)) {
            topElement = rankElementMap.get(minRank);
        }

        if (rankElementMap.has(maxRank)) {
            bottomElement = rankElementMap.get(maxRank);
        }

        if (minRank === maxRank && topElement) {
            bottomElement = topElement;
        }

        if (!topElement && bottomElement && foundRanks.length > 0) {
            const availableLowerRanks = foundRanks.filter(r => r <= maxRank).sort((a, b) => b - a);
            if (availableLowerRanks.length > 0) {
                const nearestLowerRank = availableLowerRanks[0];
                topElement = rankElementMap.get(nearestLowerRank);
            }
        }

        if (!bottomElement && topElement && foundRanks.length > 0) {
            const availableHigherRanks = foundRanks.filter(r => r >= minRank).sort((a, b) => a - b);
            if (availableHigherRanks.length > 0) {
                const nearestHigherRank = availableHigherRanks[0];
                bottomElement = rankElementMap.get(nearestHigherRank);
            }
        }

        if (!topElement || !bottomElement) {
            console.error(`CRITICAL: Could not find required rank elements!`);
            const availableInRange = foundRanks.filter(r => r >= minRank && r <= maxRank);
            if (availableInRange.length > 0) {
                const singleRank = availableInRange[0];
                const singleElement = rankElementMap.get(singleRank);
                if (singleElement) {
                    topElement = singleElement;
                    bottomElement = singleElement;
                }
            }

            if (!topElement || !bottomElement) {
                return null;
            }
        }

        const treeRect = treeGrid.getBoundingClientRect();
        const topRect = topElement.getBoundingClientRect();
        const bottomRect = bottomElement.getBoundingClientRect();


        let width = treeRect.width;

        if (!includePremium) {
            const premiumDivider = document.querySelector('.premium-divider-v');

            if (premiumDivider) {
                const dividerRect = premiumDivider.getBoundingClientRect();
                const dividerPosition = dividerRect.left - treeRect.left;
                width = Math.max(200, dividerPosition - 10);
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
                }
            }
        }

        const area = {
            x: 0,
            y: Math.max(0, topRect.top - treeRect.top - 10),
            width: Math.min(width, treeRect.width),
            height: Math.max(50, (bottomRect.bottom - topRect.top) + 20)
        };

        return area;
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