function AppDataModel() {
    var self = this;
    // Routes
    self.userInfoUrl = "/api/account/me";
    self.siteUrl = "/";

    // Route operations

    // Other private operations

    // Operations

    // Data
    self.returnUrl = self.siteUrl;

    // Data access operations
    self.setAccessToken = function (accessToken) {
        sessionStorage.setItem("accessToken", accessToken);
        localStorage.clear();
    };

    self.getAccessToken = function () {
        return sessionStorage.getItem("accessToken");
    };
}
