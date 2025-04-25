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

                            let x1 = sourceRect.right - svgRect.left;
                            let y1 = sourceRect.top + sourceRect.height / 2 - svgRect.top;
                            let x2 = targetRect.left - svgRect.left;
                            let y2 = targetRect.top + targetRect.height / 2 - svgRect.top;

                            const targetParentContainer = targetElem.closest('.tree-grid-item');

                            const sourceInFolder = sourceElem.closest('.folder-items-container');
                            const targetInFolder = targetElem.closest('.folder-items-container');

                            if (sourceInFolder && targetInFolder && sourceParentContainer === targetParentContainer) {
                                x1 = sourceRect.left + sourceRect.width / 2 - svgRect.left;
                                y1 = sourceRect.bottom - svgRect.top;
                                x2 = targetRect.left + targetRect.width / 2 - svgRect.left;
                                y2 = targetRect.top - svgRect.top;
                                console.log(`Vertical connection inside folder: ${conn.sourceElementId} -> ${conn.targetElementId}`);
                            } else {
                                console.log(`Horizontal connection: ${conn.sourceElementId} -> ${conn.targetElementId}`);
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