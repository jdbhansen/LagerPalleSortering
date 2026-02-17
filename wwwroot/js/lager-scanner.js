window.lagerScanner = {
    normalizeKeyboardLayoutDrift: function (value) {
        if (typeof value !== "string" || value.length === 0) {
            return value;
        }

        // Common mismatch when scanner and OS keyboard layouts differ.
        return value.replaceAll("æ", ":").replaceAll("Æ", ":");
    },
    init: function (productId, expiryId, quantityId, registerButtonId) {
        const product = document.getElementById(productId);
        const expiry = document.getElementById(expiryId);
        const quantity = document.getElementById(quantityId);
        const registerButton = document.getElementById(registerButtonId);

        if (!product || !expiry || !quantity || !registerButton) {
            return;
        }

        product.addEventListener("keydown", function (event) {
            if (event.key !== "Enter") {
                return;
            }

            event.preventDefault();
            expiry.focus();
            expiry.select?.();
        });

        expiry.addEventListener("keydown", function (event) {
            if (event.key !== "Enter") {
                return;
            }

            event.preventDefault();
            registerButton.click();
        });

        quantity.addEventListener("keydown", function (event) {
            if (event.key !== "Enter") {
                return;
            }

            event.preventDefault();
            registerButton.click();
        });
    },
    initPalletConfirm: function (palletInputId, confirmButtonId, confirmCountInputId) {
        const palletInput = document.getElementById(palletInputId);
        const confirmButton = document.getElementById(confirmButtonId);
        const confirmCountInput = document.getElementById(confirmCountInputId);

        if (!palletInput || !confirmButton) {
            return;
        }

        const normalizePalletInput = function () {
            const normalized = window.lagerScanner.normalizeKeyboardLayoutDrift(palletInput.value);
            if (normalized === palletInput.value) {
                return;
            }

            palletInput.value = normalized;
            palletInput.dispatchEvent(new Event("input", { bubbles: true }));
            palletInput.dispatchEvent(new Event("change", { bubbles: true }));
        };

        palletInput.addEventListener("input", normalizePalletInput);

        palletInput.addEventListener("keydown", function (event) {
            if (event.key !== "Enter") {
                return;
            }

            normalizePalletInput();
            event.preventDefault();
            confirmButton.click();
        });

        if (confirmCountInput) {
            confirmCountInput.addEventListener("keydown", function (event) {
                if (event.key !== "Enter") {
                    return;
                }

                event.preventDefault();
                confirmButton.click();
            });
        }
    },
    initHotkeys: function (options) {
        if (!options) {
            return;
        }

        const byId = function (id) {
            if (!id) {
                return null;
            }

            return document.getElementById(id);
        };

        if (window.__lagerHotkeysHandler) {
            document.removeEventListener("keydown", window.__lagerHotkeysHandler, true);
        }

        const handler = function (event) {
            const key = (event.key || "").toLowerCase();
            const alt = event.altKey === true;
            // Alt+1: fokus varenummer
            if (alt && key === "1") {
                event.preventDefault();
                byId(options.productInputId)?.focus();
                byId(options.productInputId)?.select?.();
                return;
            }

            // Alt+2: fokus palle-scan
            if (alt && key === "2") {
                event.preventDefault();
                byId(options.palletInputId)?.focus();
                byId(options.palletInputId)?.select?.();
                return;
            }

            // Alt+R: registrer kolli
            if (alt && key === "r") {
                event.preventDefault();
                byId(options.registerButtonId)?.click();
                return;
            }

            // Alt+B: bekræft flyt
            if (alt && key === "b") {
                event.preventDefault();
                byId(options.confirmButtonId)?.click();
                return;
            }

            // Alt+U: fortryd seneste
            if (alt && key === "u") {
                event.preventDefault();
                byId(options.undoButtonId)?.click();
                return;
            }

            // Esc: annuller ryd database advarsel
            if (key === "escape") {
                const cancelBtn = byId(options.clearCancelButtonId);
                if (cancelBtn) {
                    event.preventDefault();
                    cancelBtn.click();
                }
            }
        };

        window.__lagerHotkeysHandler = handler;
        document.addEventListener("keydown", handler, true);
    },
    focus: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) {
            return;
        }

        el.focus();
        el.select?.();
    }
};

window.lagerPrint = {
    printAndClosePopupTab: function () {
        // Only auto-close tabs opened from the main app window.
        const closeIfPopup = function () {
            if (window.opener && !window.opener.closed) {
                window.close();
            }
        };

        const onAfterPrint = function () {
            window.removeEventListener("afterprint", onAfterPrint);
            closeIfPopup();
        };

        window.addEventListener("afterprint", onAfterPrint);
        window.print();

        // Fallback for browsers where afterprint is unreliable.
        window.setTimeout(function () {
            window.removeEventListener("afterprint", onAfterPrint);
            closeIfPopup();
        }, 1200);
    },
    closePopupTabOrNavigateHome: function () {
        if (window.opener && !window.opener.closed) {
            window.close();
            return;
        }

        window.location.href = "/";
    }
};
