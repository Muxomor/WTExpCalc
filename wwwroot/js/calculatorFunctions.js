window.calculatorFunctions = (function () {
    const storageKey = 'wtExpCalcCalculatorLayout';
    const minWidth = 250;
    const minHeight = 360;
    const visibleMargin = 40;

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function readLayout() {
        try {
            const raw = localStorage.getItem(storageKey);
            if (!raw) return null;

            const data = JSON.parse(raw);
            if (typeof data.left !== 'number' || typeof data.top !== 'number' ||
                typeof data.width !== 'number' || typeof data.height !== 'number') {
                return null;
            }

            return data;
        } catch (error) {
            return null;
        }
    }

    function saveLayout(element) {
        const rect = element.getBoundingClientRect();
        try {
            localStorage.setItem(storageKey, JSON.stringify({
                left: Math.round(rect.left),
                top: Math.round(rect.top),
                width: Math.round(rect.width),
                height: Math.round(rect.height)
            }));
        } catch (error) {
            // localStorage может быть недоступен, размер просто не сохранится.
        }
    }

    function maxWidth() {
        return Math.max(minWidth, window.innerWidth - visibleMargin * 2);
    }

    function maxHeight() {
        return Math.max(minHeight, window.innerHeight - visibleMargin * 2);
    }

    function applyLayout(element, layout) {
        const width = clamp(layout.width, minWidth, maxWidth());
        const height = clamp(layout.height, minHeight, maxHeight());
        const left = clamp(layout.left, visibleMargin - width, window.innerWidth - visibleMargin);
        const top = clamp(layout.top, 0, window.innerHeight - visibleMargin);

        element.style.width = width + 'px';
        element.style.height = height + 'px';
        element.style.left = left + 'px';
        element.style.top = top + 'px';
        element.style.right = 'auto';
        element.style.bottom = 'auto';
    }

    function pinToLeftTop(element) {
        const rect = element.getBoundingClientRect();
        element.style.left = rect.left + 'px';
        element.style.top = rect.top + 'px';
        element.style.right = 'auto';
        element.style.bottom = 'auto';
        return rect;
    }

    function attachDrag(element, handle) {
        if (!handle) return;
        handle.style.touchAction = 'none';

        handle.addEventListener('pointerdown', function (event) {
            if (event.button !== 0) return;
            event.preventDefault();

            const rect = pinToLeftTop(element);
            const offsetX = event.clientX - rect.left;
            const offsetY = event.clientY - rect.top;
            handle.setPointerCapture(event.pointerId);

            function onMove(moveEvent) {
                const left = clamp(moveEvent.clientX - offsetX, visibleMargin - rect.width, window.innerWidth - visibleMargin);
                const top = clamp(moveEvent.clientY - offsetY, 0, window.innerHeight - visibleMargin);
                element.style.left = left + 'px';
                element.style.top = top + 'px';
            }

            function onEnd() {
                handle.removeEventListener('pointermove', onMove);
                handle.removeEventListener('pointerup', onEnd);
                handle.removeEventListener('pointercancel', onEnd);
                saveLayout(element);
            }

            handle.addEventListener('pointermove', onMove);
            handle.addEventListener('pointerup', onEnd);
            handle.addEventListener('pointercancel', onEnd);
        });
    }

    function attachResize(element, grip) {
        if (!grip) return;
        grip.style.touchAction = 'none';

        grip.addEventListener('pointerdown', function (event) {
            if (event.button !== 0) return;
            event.preventDefault();
            event.stopPropagation();

            const rect = pinToLeftTop(element);
            const right = rect.right;
            const bottom = rect.bottom;
            const startX = event.clientX;
            const startY = event.clientY;
            grip.setPointerCapture(event.pointerId);

            function onMove(moveEvent) {
                const width = clamp(rect.width - (moveEvent.clientX - startX), minWidth, Math.max(minWidth, right));
                const height = clamp(rect.height - (moveEvent.clientY - startY), minHeight, Math.max(minHeight, bottom));
                element.style.width = width + 'px';
                element.style.height = height + 'px';
                element.style.left = (right - width) + 'px';
                element.style.top = (bottom - height) + 'px';
            }

            function onEnd() {
                grip.removeEventListener('pointermove', onMove);
                grip.removeEventListener('pointerup', onEnd);
                grip.removeEventListener('pointercancel', onEnd);
                saveLayout(element);
            }

            grip.addEventListener('pointermove', onMove);
            grip.addEventListener('pointerup', onEnd);
            grip.addEventListener('pointercancel', onEnd);
        });
    }

    return {
        init: function (windowElement, headerElement, gripElement) {
            if (!windowElement) return;

            const layout = readLayout();
            if (layout) {
                applyLayout(windowElement, layout);
            }

            if (windowElement.dataset.calculatorInit === '1') return;
            windowElement.dataset.calculatorInit = '1';

            attachDrag(windowElement, headerElement);
            attachResize(windowElement, gripElement);
        },

        getCaret: function (inputElement) {
            if (!inputElement) return 0;
            try {
                if (typeof inputElement.selectionStart === 'number') {
                    return inputElement.selectionStart;
                }
            } catch (error) {
                // Некоторые типы input не поддерживают selectionStart.
            }

            return inputElement.value ? inputElement.value.length : 0;
        },

        setCaret: function (inputElement, position) {
            if (!inputElement) return;
            try {
                inputElement.focus();
                const caret = clamp(position, 0, inputElement.value ? inputElement.value.length : 0);
                inputElement.setSelectionRange(caret, caret);
            } catch (error) {
                // Фокус не критичен.
            }
        }
    };
})();
