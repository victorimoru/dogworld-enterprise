// The SDK snippet loads asynchronously. The Blazor wrapper must not call updateCfg
// until the real SDK has replaced the snippet's queue-only placeholder.
window.dogWorldTelemetry = {
    waitForSdk: function (timeoutMs) {
        return new Promise(function (resolve) {
            const deadline = Date.now() + timeoutMs;
            function check() {
                if (typeof window.appInsights?.updateCfg === "function") {
                    resolve(true);
                } else if (Date.now() >= deadline) {
                    console.warn("Application Insights SDK did not load; continuing without browser telemetry.");
                    resolve(false);
                } else {
                    setTimeout(check, 50);
                }
            }
            check();
        });
    }
};
