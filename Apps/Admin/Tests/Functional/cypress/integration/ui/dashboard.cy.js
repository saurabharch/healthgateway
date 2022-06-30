import { verifyTestingEnvironment } from "../../support/functions/environment";

describe("Dashboard", () => {
    beforeEach(() => {
        verifyTestingEnvironment();

        cy.intercept("GET", "**/Dashboard/RegisteredCount*", {
            fixture: "DashboardService/registered-count.json",
        });

        cy.intercept("GET", "**/Dashboard/LoggedInCount*", {
            fixture: "DashboardService/logged-in-count.json",
        });

        cy.intercept("GET", "**/Dashboard/DependentCount*", {
            fixture: "DashboardService/dependent-count.json",
        });

        // used to calculate [data-testid=average-rating]
        cy.intercept("GET", "**/Dashboard/Ratings/Summary*", {
            fixture: "DashboardService/summary.json",
        });

        // matches [data-testid=total-unique-users]
        cy.intercept("GET", "**/Dashboard/RecurringUsers?days=3*", {
            body: 2,
        });

        cy.log("Logging in.");
        cy.login(Cypress.env("idir_username"), Cypress.env("idir_password"));
    });

    it("Verify dashboards counts.", () => {
        cy.log("Dashboard test started.");
        cy.get("[data-testid=total-registered-users]").contains(6);
        cy.get("[data-testid=total-dependents]").contains(2);
        cy.get("[data-testid=average-rating]").contains("4.00");
        cy.get("[data-testid=total-unique-users]").contains(2);

        cy.get("[data-testid=daily-data-table]")
            .first()
            .within(() => {
                cy.get(
                    "[data-testid=daily-data-total-registered-users]"
                ).contains("2");
                cy.get(
                    "[data-testid=daily-data-total-logged-in-users]"
                ).contains("6");
                cy.get("[data-testid=daily-data-dependents]").contains("2");
            });

        cy.log("Change value in unique days input field.");
        cy.intercept("GET", "**/Dashboard/RecurringUsers?days=5*", {
            body: 0,
        });
        cy.log("Updating unique days input value.");
        cy.get("[data-testid=unique-days-input]").clear().type(5);
        cy.get("[data-testid=total-unique-users]").click().contains(0);

        cy.intercept("GET", "**/Dashboard/RecurringUsers?days=2*", {
            body: 3,
        });
        cy.log("Updating unique days input value.");
        cy.get("[data-testid=unique-days-input]").clear().type(2);
        cy.get("[data-testid=total-unique-users]").click().contains(3);

        cy.log("Clicking refresh button.");
        cy.intercept("GET", "**/Dashboard/RegisteredCount*", {
            fixture: "DashboardService/registered-count-refresh.json",
        });

        cy.intercept("GET", "**/Dashboard/LoggedInCount*", {
            fixture: "DashboardService/logged-in-count-refresh.json",
        });

        cy.intercept("GET", "**/Dashboard/DependentCount*", {
            fixture: "DashboardService/dependent-count-refresh.json",
        });

        cy.intercept("GET", "**/Dashboard/Ratings/Summary*", {
            fixture: "DashboardService/summary-refresh.json",
        });

        cy.intercept("GET", "**/Dashboard/RecurringUsers?days=2*", {
            body: 10,
        });

        cy.get("[data-testid=refresh-btn]").click();
        cy.get("[data-testid=total-registered-users]").contains(7);
        cy.get("[data-testid=total-dependents]").contains(3);
        cy.get("[data-testid=average-rating]").contains("3.00");
        cy.get("[data-testid=total-unique-users]").contains(10);

        cy.get("[data-testid=daily-data-table]")
            .first()
            .within(() => {
                cy.get(
                    "[data-testid=daily-data-total-registered-users]"
                ).contains("3");
                cy.get(
                    "[data-testid=daily-data-total-logged-in-users]"
                ).contains("7");
                cy.get("[data-testid=daily-data-dependents]").contains("3");
            });

        cy.log("Dashboard test finished.");
    });
});