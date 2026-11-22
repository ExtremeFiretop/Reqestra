import { userPreferencesPage as Page } from "@/integration/page-objects";

describe("User Preferences tab initialization", () => {
    it("keeps local-account Security fields accessible alongside Notifications", () => {
        cy.login();
        Page.visit();

        // The Security tab should appear only once the account type is loaded.
        Page.securityTab.click();
        Page.security.currentPassword.should("be.visible");
        Page.security.email.should("be.visible");
        Page.security.newPassword.should("be.visible");

        cy.contains('[role="tab"]', "Notifications").click();
        cy.contains("h2", "Email notifications").should("be.visible");
    });
});
