import { verifyTestingEnvironment } from "../../support/functions/environment";

const email = "fakeemail@healthgateway.gov.bc.ca";
const emailNotFound = "fakeemail_noresults@healthgateway.gov.bc.ca";
const emailHdid = "DEV4FPEGCXG2NB5K2USBL52S66SC3GOUHWRP3GTXR2BTY5HEC4YA";
const phn = "9735353315";
const phnInvalid = "9999999999";
const phnNotFound = "9735361219";
const hdid = "P6FFO433A5WPMVTGM7T4ZVWBKCSVNAYGTWTU3J2LWMGUMERKI72A";
const hdidNotFound = "P123456789";
const sms = "2501234567";
const smsNotFound = "5551234567";

function verifyTableResults(queryType) {
    // Expecting 1 row to return but you also need to consider the table headers. As a result, length should be 2.
    cy.get("[data-testid=message-verification-table]")
        .find("tr")
        .should("have.length", 2);

    // Hdid is unique and is used as a unique identifier for each row in the table.
    cy.get(`[data-testid=message-verification-table-hdid-${hdid}]`).contains(
        hdid
    );

    if (queryType === "SMS") {
        cy.get(`[data-testid=message-verification-table-phn-${hdid}]`).should(
            "be.empty"
        );
    } else {
        cy.get(`[data-testid=message-verification-table-phn-${hdid}]`).contains(
            phn
        );
    }
}

describe("Support", () => {
    beforeEach(() => {
        verifyTestingEnvironment();

        // PHN with results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=9735353315&queryType=PHN",
            {
                fixture: "SupportService/users-phn.json",
            }
        );

        // HDID with results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=P6FFO433A5WPMVTGM7T4ZVWBKCSVNAYGTWTU3J2LWMGUMERKI72A&queryType=HDID",
            {
                fixture: "SupportService/users-hdid.json",
            }
        );

        // SMS with results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=2501234567&queryType=SMS",
            {
                fixture: "SupportService/users-sms.json",
            }
        );

        // Email with results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=fakeemail%40healthgateway.gov.bc.ca&queryType=Email",
            {
                fixture: "SupportService/users-email.json",
            }
        );

        // PHN no results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=9735361219&queryType=PHN",
            {
                fixture: "SupportService/users-phn-no-results.json",
            }
        );

        // HDID no results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=P123456789&queryType=HDID",
            {
                fixture: "SupportService/users-hdid-no-results.json",
            }
        );

        // SMS no results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=5551234567&queryType=SMS",
            {
                fixture: "SupportService/users-sms-no-results.json",
            }
        );

        // Email no results
        cy.intercept(
            "GET",
            "**/Support/Users?queryString=fakeemail_noresults%40healthgateway.gov.bc.ca&queryType=Email",
            {
                fixture: "SupportService/users-email-no-results.json",
            }
        );

        cy.log("Logging in.");
        cy.login(Cypress.env("idir_username"), Cypress.env("idir_password"));

        cy.log("Navigate to support page.");
        cy.visit("/support");
    });

    afterEach(() => {
        cy.log("Logging out.");
        cy.logout();
    });

    it("Verify support query.", () => {
        cy.log("Verify support query test started.");

        // Search by PHN
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("PHN").click();
        cy.get("[data-testid=query-input]").clear().type(phn);
        cy.get("[data-testid=search-btn]").click();
        verifyTableResults("PHN");

        // Search by HDID.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("HDID").click();
        cy.get("[data-testid=query-input]").clear().type(hdid);
        cy.get("[data-testid=search-btn]").click();
        verifyTableResults("HDID");

        // Search by SMS.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("SMS").click();
        cy.get("[data-testid=query-input]").clear().type(sms);
        cy.get("[data-testid=search-btn]").click();
        verifyTableResults("SMS");

        // Search by Email.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("Email").click();
        cy.get("[data-testid=query-input]").clear().type(email);
        cy.get("[data-testid=search-btn]").click();
        cy.get("[data-testid=message-verification-table]")
            .find("tr")
            .should("have.length", 2);
        cy.get(
            `[data-testid=message-verification-table-hdid-${emailHdid}]`
        ).contains(emailHdid);
        cy.get(
            `[data-testid=message-verification-table-phn-${emailHdid}]`
        ).should("be.empty");

        cy.log("Verify support query test finished.");
    });

    it("Verify no results phn query.", () => {
        cy.log("Verify phn returns no results test started.");

        // Search with PHN not found in client registry.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("PHN").click();
        cy.get("[data-testid=query-input]").clear().type(phnNotFound);
        cy.get("[data-testid=search-btn]").click();
        cy.get("[data-testid=banner-feedback-error-message]").should(
            "be.visible"
        );
        // Expect no results for email.
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        // Close banner
        cy.get("[data-testid=banner-feedback-error-message]").within(() => {
            cy.get("button").parent(".mud-alert-close").click();
        });

        cy.log("Verify phn returns no results test finished.");
    });

    it("Verify no results hdid query.", () => {
        cy.log("Verify hdid returns no results test started.");

        // Search with PHN not found in client registry.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("HDID").click();
        cy.get("[data-testid=query-input]").clear().type(hdidNotFound);
        cy.get("[data-testid=search-btn]").click();
        cy.get("[data-testid=banner-feedback-error-message]").should(
            "be.visible"
        );
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        // Close banner
        cy.get("[data-testid=banner-feedback-error-message]").within(() => {
            cy.get("button").parent(".mud-alert-close").click();
        });

        cy.log("Verify hdid returns no results test finished.");
    });

    it("Verify no results sms query.", () => {
        cy.log("Verify sms returns no results test started.");

        // Search with SMS not found.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("SMS").click();
        cy.get("[data-testid=query-input]").clear().type(smsNotFound);
        cy.get("[data-testid=search-btn]").click();
        cy.get("[data-testid=message-verification-table]")
            .find("tr")
            .should("have.length", 1);

        cy.log("Verify sms returns no results test finished.");
    });

    it("Verify no results email query.", () => {
        cy.log("Verify email returns no results test started.");

        // Search with Email not found.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("Email").click();
        cy.get("[data-testid=query-input]").clear().type(emailNotFound);
        cy.get("[data-testid=search-btn]").click();
        cy.get("[data-testid=message-verification-table]")
            .find("tr")
            .should("have.length", 1);

        cy.log("Verify email returns no results test finished.");
    });

    it("Verify invalid phn.", () => {
        cy.log("Verify invalid phn test started.");

        // Search by invalid PHN.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("PHN").click();
        cy.get("[data-testid=query-input]").clear().type(phnInvalid);
        cy.get("[data-testid=search-btn]").click();
        cy.get(".d-flex").contains("Invalid PHN").should("be.visible");
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        cy.log("Verify invalid phn test finished.");
    });

    it("Verify required paramater.", () => {
        cy.log("Verify required parameter test started.");

        // Search by empty PHN.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("PHN").click();
        cy.get("[data-testid=query-input]").clear();
        cy.get("[data-testid=search-btn]").click();
        cy.get(".d-flex")
            .contains("Search parameter is required")
            .should("be.visible");
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        // Search by empty HDID.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("HDID").click();
        cy.get("[data-testid=query-input]").clear();
        cy.get("[data-testid=search-btn]").click();
        cy.get(".d-flex")
            .contains("Search parameter is required")
            .should("be.visible");
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        // Search by empty SMS.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("SMS").click();
        cy.get("[data-testid=query-input]").clear();
        cy.get("[data-testid=search-btn]").click();
        cy.get(".d-flex")
            .contains("Search parameter is required")
            .should("be.visible");
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        // Search by empty Email.
        cy.get("[data-testid=query-type-select]").click();
        cy.get("[data-testid=query-type]").contains("Email").click();
        cy.get("[data-testid=query-input]").clear();
        cy.get("[data-testid=search-btn]").click();
        cy.get(".d-flex")
            .contains("Search parameter is required")
            .should("be.visible");
        cy.get("[data-testid=message-verification-table]").should("not.exist");

        cy.log("Verify required parameter test finished.");
    });
});