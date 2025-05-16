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
                console.log(`Popup ${popupId} fits below. Space: ${spaceBelow}, PopupHeight: ${popupHeight}`);
            } else if (popupHeight <= spaceAbove) {
                top = targetRect.top - containerRect.top + (containerElement.scrollTop || 0) - popupHeight - 5;
                console.log(`Popup ${popupId} fits ABOVE. Space: ${spaceAbove}, PopupHeight: ${popupHeight}`);
            } else {
                top = targetRect.bottom - containerRect.top + (containerElement.scrollTop || 0) + 5;
                const availableHeight = viewportHeight - summaryPanelHeight - (targetRect.bottom + 5);
                if (availableHeight < popupHeight && availableHeight > 50) { 
                    console.log(`Popup ${popupId} doesn't fit well, using default bottom position. Available: ${availableHeight}`);
                } else {
                    console.log(`Popup ${popupId} placed below by default due to insufficient space above either.`);
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
            console.log(`Positioned popup ${popupId} at top: ${top}, left: ${finalLeft}`);
        });
    },

    copyTextToClipboard: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            console.log('Async: Copying to clipboard was successful!');
        }, function (err) {
            console.error('Async: Could not copy text: ', err);
            alert("Не удалось скопировать текст."); 
        });
    }, copyTextToClipboard_fallback: function (text) {
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
                console.log('Fallback: Copying to clipboard was successful!');
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
                            console.error("Error calculating line coordinates for conn:", conn, e);
                            if (line) line.style.visibility = 'hidden';
                        }
                    } else {
                        if (line) line.style.visibility = 'hidden';
                    }
                });
            });
        });
    }
};