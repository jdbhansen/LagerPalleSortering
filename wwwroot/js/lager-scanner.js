window.lagerScanner = {
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

        palletInput.addEventListener("keydown", function (event) {
            if (event.key !== "Enter") {
                return;
            }

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
    focus: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) {
            return;
        }

        el.focus();
        el.select?.();
    }
};
