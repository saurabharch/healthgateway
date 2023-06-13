import { performSearch } from "../../utilities/supportUtilities";
import { getTableRows } from "../../utilities/sharedUtilities";

const hdid = "P6FFO433A5WPMVTGM7T4ZVWBKCSVNAYGTWTU3J2LWMGUMERKI72A";
const switchName = "Immunization";
const auditBlockReason = "Test block reason";
const auditUnblockReason = "Test unblock reason";

function checkAgentAuditHistory() {
    cy.get("[data-testid=agent-audit-history-title]")
        .should("be.visible")
        .click();

    cy.get("[data-testid=agent-audit-history-table]").should("be.visible");

    return cy.get("[data-testid=agent-audit-history-count]").invoke("text");
}

describe("Patient details page as admin", () => {
    beforeEach(() => {
        cy.login(
            Cypress.env("keycloak_username"),
            Cypress.env("keycloak_password"),
            "/support"
        );
    });

    it("Verify message verification", () => {
        performSearch("HDID", hdid);

        cy.get("[data-testid=user-table]")
            .find("tbody tr.mud-table-row")
            .first()
            .click();

        cy.get("[data-testid=patient-name]").should("be.visible");
        cy.get("[data-testid=patient-dob]").should("be.visible");
        cy.get("[data-testid=patient-phn]").should("be.visible");
        cy.get("[data-testid=patient-hdid]")
            .should("be.visible")
            .contains(hdid);
        cy.get("[data-testid=patient-physical-address]").should("be.visible");
        cy.get("[data-testid=patient-mailing-address]").should("be.visible");
        cy.get("[data-testid=profile-created-datetime]").should("be.visible");
        cy.get("[data-testid=profile-last-login-datetime]").should(
            "be.visible"
        );
        getTableRows("[data-testid=messaging-verification-table]").should(
            "have.length",
            2
        );
    });

    it("Verify block access initial", () => {
        performSearch("HDID", hdid);
        cy.get("[data-testid=user-table]")
            .find("tbody tr.mud-table-row")
            .first()
            .click();

        cy.get("[data-testid=block-access-switches]").should("be.visible");
        cy.get(`[data-testid*="block-access-switch"]`).should(
            "not.have.attr",
            "readonly"
        );
        cy.get("[data-testid=block-access-cancel]").should("not.exist");
        cy.get("[data-testid=block-access-save]").should("not.exist");
    });

    it("Verify block access change can be cancelled", () => {
        performSearch("HDID", hdid);
        cy.get("[data-testid=user-table]")
            .find("tbody tr.mud-table-row")
            .first()
            .click();

        cy.get("[data-testid=block-access-loader]").should("not.be.visible");

        cy.get(`[data-testid=block-access-switch-${switchName}]`)
            .should("exist")
            .click();

        cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
            "be.checked"
        );

        cy.get("[data-testid=block-access-save]").should("be.visible");
        cy.get("[data-testid=block-access-cancel]")
            .should("be.visible")
            .click();

        cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
            "not.be.checked"
        );
    });

    it("Verify block access can be blocked with audit reason.", () => {
        performSearch("HDID", hdid);
        cy.get("[data-testid=user-table]")
            .find("tbody tr.mud-table-row")
            .first()
            .click();

        cy.get("[data-testid=block-access-loader]").should("not.be.visible");

        checkAgentAuditHistory().then((presaveCount) => {
            cy.get(`[data-testid=block-access-switch-${switchName}]`)
                .should("exist")
                .click();

            cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
                "be.checked"
            );

            cy.get("[data-testid=block-access-cancel]").should(
                "exist",
                "be.visible"
            );
            cy.get("[data-testid=block-access-save]")
                .should("exist", "be.visible")
                .click();

            cy.get("[data-testid=audit-reason-input")
                .should("be.visible")
                .type(auditBlockReason);

            cy.get("[data-testid=audit-confirm-button]")
                .should("be.visible")
                .click({ force: true });

            cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
                "be.checked"
            );

            // Check agent audit history
            checkAgentAuditHistory().then((postsaveCount) => {
                expect(Number(postsaveCount)).to.equal(
                    Number(presaveCount) + 1
                );
            });
        });
    });

    it("Verify block access can be unblocked with audit reason.", () => {
        performSearch("HDID", hdid);
        cy.get("[data-testid=user-table]")
            .find("tbody tr.mud-table-row")
            .first()
            .click();

        cy.get("[data-testid=block-access-loader]").should("not.be.visible");

        cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
            "be.checked"
        );

        cy.get(`[data-testid=block-access-switch-${switchName}]`)
            .should("exist")
            .click();

        checkAgentAuditHistory().then((presaveCount) => {
            cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
                "not.be.checked"
            );

            cy.get("[data-testid=block-access-cancel]").should(
                "exist",
                "be.visible"
            );
            cy.get("[data-testid=block-access-save]")
                .should("exist", "be.visible")
                .click();

            cy.get("[data-testid=audit-reason-input")
                .should("be.visible")
                .type(auditUnblockReason);

            cy.get("[data-testid=audit-confirm-button]")
                .should("be.visible")
                .click({ force: true });

            cy.get(`[data-testid=block-access-switch-${switchName}]`).should(
                "not.be.checked"
            );

            // Check agent audit history
            checkAgentAuditHistory().then((postsaveCount) => {
                expect(Number(postsaveCount)).to.equal(
                    Number(presaveCount) + 1
                );
            });
        });
    });
});

describe("Patient details page as reviewer", () => {
    beforeEach(() => {
        cy.login(
            Cypress.env("keycloak_reviewer_username"),
            Cypress.env("keycloak_password"),
            "/support"
        );
    });

    // verify that the reviewer cannot use the block access controls
    it("Verify block access readonly", () => {
        performSearch("HDID", hdid);
        cy.get("[data-testid=user-table]")
            .find("tbody tr.mud-table-row")
            .first()
            .click();

        cy.get(`[data-testid*="block-access-switch-"]`).each(($el) => {
            // follow the mud tag structure to find the mud-readonly class
            cy.wrap($el).parent().parent().should("have.class", "mud-readonly");
        });

        // Clicke any switch and check if the dirty state has exposed the save and cancel buttons
        cy.get(`[data-testid=block-access-switch-${switchName}]`)
            .should("exist")
            .click();

        cy.get("[data-testid=block-access-cancel]").should("not.exist");
        cy.get("[data-testid=block-access-save]").should("not.exist");
    });
});