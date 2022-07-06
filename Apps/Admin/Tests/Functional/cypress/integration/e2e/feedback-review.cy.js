import { verifyTestingEnvironment } from "../../support/functions/environment";

const suggestionTag = "suggestion";
const questionTag = "question";

function validateTableLoad(tableSelector) {
    cy.get(tableSelector)
        .find(".mud-table-loading-progress")
        .should("be.visible");
    cy.get(tableSelector)
        .find(".mud-table-loading-progress")
        .should("not.exist");
}

function validateTableRowCount(tableSelector, count) {
    cy.log(`Validating table contains ${count} rows of data.`);
    cy.get(tableSelector)
        .find("tbody tr.mud-table-row")
        .should("have.length", count);
}

describe("Feedback Review", () => {
    beforeEach(() => {
        verifyTestingEnvironment();

        cy.log("Logging in.");
        cy.login(Cypress.env("idir_username"), Cypress.env("idir_password"));

        cy.log("Navigating to feedback review page.");
        cy.visit("/feedback");
    });

    afterEach(() => {
        cy.log("Logging out.");
        cy.logout();
    });

    it("Tag and Feedback Table Functionality", () => {
        cy.log("Validating data initialized by seed script.");
        cy.get("[data-testid=tag-collection]").should("exist");
        cy.get("[data-testid=tag-collection-item]").should("not.exist");
        validateTableRowCount("[data-testid=feedback-table]", 2);

        // alias for first row of table
        cy.get("[data-testid=feedback-table] tbody tr.mud-table-row")
            .first()
            .as("firstRow");

        cy.log("Adding tags.");
        cy.get("[data-testid=add-tag-input]").clear().type(suggestionTag);
        cy.get("[data-testid=add-tag-button]").click();
        cy.get("[data-testid=tags-updating-indicator").should("be.visible");
        cy.get("[data-testid=tag-collection-item]").contains(suggestionTag);
        cy.get("[data-testid=add-tag-input]").clear().type(questionTag);
        cy.get("[data-testid=add-tag-button]").click();
        cy.get("[data-testid=tags-updating-indicator").should("be.visible");
        cy.get("[data-testid=tag-collection-item]").contains(questionTag);

        cy.log("Assigning tags.");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-tag-select]").click();
        });
        cy.get("[data-testid=feedback-tag]").contains(suggestionTag).click();
        validateTableLoad("[data-testid=feedback-table]");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-tag-select]").should(
                "have.value",
                suggestionTag
            );
        });

        cy.log("Filtering tags.");
        cy.get("[data-testid=tag-collection-item]")
            .contains(suggestionTag)
            .click();
        validateTableRowCount("[data-testid=feedback-table]", 1);
        cy.get("[data-testid=tag-collection-item]")
            .contains(suggestionTag)
            .click();
        validateTableRowCount("[data-testid=feedback-table]", 2);

        cy.log("Validating tags cannot be removed while assigned to feedback.");
        cy.get("[data-testid=tag-collection-item]")
            .contains(suggestionTag)
            .children("button")
            .click();
        cy.get("[data-testid=tag-collection-item]")
            .contains(suggestionTag)
            .should("be.visible");

        cy.log("Unassigning tags.");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-tag-select]").click();
        });
        cy.get("[data-testid=feedback-tag]").contains(suggestionTag).click();
        validateTableLoad("[data-testid=feedback-table]");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-tag-select]").should(
                "not.have.value",
                suggestionTag
            );
        });

        cy.log("Removing tags.");
        cy.get("[data-testid=tag-collection-item]")
            .contains(suggestionTag)
            .children("button")
            .click();
        cy.get("[data-testid=tags-updating-indicator").should("be.visible");
        cy.get("[data-testid=tag-collection-item]")
            .contains(questionTag)
            .children("button")
            .click();
        cy.get("[data-testid=tags-updating-indicator").should("be.visible");
        cy.get("[data-testid=tag-collection-item]").should("not.exist");

        cy.log("Reviewing feedback.");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-review-button]")
                .should("have.class", "mud-error-text")
                .click();
        });
        validateTableLoad("[data-testid=feedback-table]");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-review-button]")
                .should("have.class", "mud-success-text")
                .click();
        });

        cy.log("Looking up feedback author.");
        cy.get("@firstRow").within(() => {
            cy.get("[data-testid=feedback-person-search-button]")
                .should("be.visible")
                .click();
        });
        cy.location("pathname").should("eq", "/support");
        validateTableLoad("[data-testid=message-verification-table]");
        validateTableRowCount("[data-testid=message-verification-table]", 1);
    });
});
