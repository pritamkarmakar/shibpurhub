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
        localStorage.clear();
        localStorage.setItem("accessToken", accessToken);
        //sessionStorage.setItem("accessToken", accessToken);
    };

    self.getAccessToken = function () {
        //return sessionStorage.getItem("accessToken");
        return localStorage.getItem("accessToken");
    };
}
