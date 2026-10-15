
import { discoverPage as Page } from "@/integration/page-objects";

describe("Discover Recently Requested Tests", () => {
  beforeEach(() => {
    cy.login();
  });

  it("Requested Movie Is Displayed", () => {

    cy.requestMovie(315635);
    cy.intercept("GET", "**/v2/Requests/recentlyRequested").as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("315635");
      card.verifyTitle("Spider-Man: Homecoming");
      card.status.should('contain.text', 'Approved'); 
    });
  });

  it("Requested Movie Is Pending Approval", () => {

    cy.requestMovie(626735);

    cy.intercept("GET", "**/v2/Requests/recentlyRequested", (req) => {
      req.reply((res) => {
        const body = res.body;
        const movie = body[0];
        movie.available = false;
        movie.approved = false;

        body[0] = movie;
        res.send(body);
      });
    }).as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("626735");
      card.verifyTitle("Dog");
      card.status.should('contain.text', 'Pending');
      card.reveal();
      card.approveButton.should('be.visible');
    });
  });

  it("Requested Movie Is Available", () => {

    cy.requestMovie(675353);

    cy.intercept("GET", "**/v2/Requests/recentlyRequested", (req) => {
      req.reply((res) => {
        const body = res.body;
        const movie = body[0];
        movie.available = true;

        body[0] = movie;
        res.send(body);
      });
    }).as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("675353");
      card.verifyTitle("Sonic the Hedgehog 2");
      card.status.should('contain.text', 'Available'); // Because admin auto request
      card.approveButton.should('not.exist');
    });
  });

  it("Requested TV Is Available", () => {

    cy.requestAllTv(135647);

    cy.intercept("GET", "**/v2/Requests/recentlyRequested", (req) => {
      req.reply((res) => {
        const body = res.body;
        const tv = body[0];
        tv.available = true;

        body[0] = tv;
        res.send(body);
      });
    }).as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("135647");
      card.verifyTitle("2 Good 2 Be True");
      card.status.should('contain.text', 'Available');
      card.approveButton.should('not.exist');
    });
  });

  it("Requested TV Is Partially Available", () => {

    cy.requestAllTv(158415);

    cy.intercept("GET", "**/v2/Requests/recentlyRequested", (req) => {
      req.reply((res) => {
        const body = res.body;
        const tv = body[0];
        tv.tvPartiallyAvailable = true;

        body[0] = tv;
        res.send(body);
      });
    }).as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("158415");
      card.verifyTitle("Pantanal");
      card.status.should('contain.text', 'Partially Available');
      card.approveButton.should('not.exist');
    });
  });

  it("Requested TV Is Pending", () => {
    cy.requestAllTv(60574);

    cy.intercept("GET", "**/v2/Requests/recentlyRequested", (req) => {
      req.reply((res) => {
        const body = res.body;
        const tv = body[0];
        tv.approved = false;

        body[0] = tv;
        res.send(body);
      });
    }).as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("60574");
      card.verifyTitle("Peaky Blinders");
      card.status.should('contain.text', 'Pending');
      card.reveal();
      card.approveButton.should('be.visible');
    });
  });

  it("Requested TV Is Displayed", () => {

    cy.requestAllTv(66732);
    cy.intercept("GET", "**/v2/Requests/recentlyRequested").as("response");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("66732");
      card.verifyTitle("Stranger Things");
      card.status.should('contain.text', 'Approved'); // Because admin auto request
    });
  });

  it("Approve Requested Movie", () => {

    const username = `pendingMovie${Date.now()}${Cypress._.random(1000, 9999)}`;
    const password = "password";

    // Create the request as a normal user so the database contains a genuinely
    // pending request. Admin-created requests are auto-approved, and mocking
    // only the GET response leaves the UI out of sync with the persisted state.
    cy.createUser(username, password, [{
      value: "requestmovie",
      Enabled: "true",
    }]);
    cy.loginWithCreds(username, password);
    cy.requestMovie(55341);
    cy.login();

    cy.intercept("GET", "**/v2/Requests/recentlyRequested").as("response");
    cy.intercept("POST", "**/v1/Request/Movie/Approve").as("approveCall");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("55341");
      card.status.should('contain.text', 'Pending');
      card.reveal();
      // The card scales on hover while its action buttons animate into view. A
      // Cypress .click() moves the virtual pointer onto the button and can race
      // that transform, causing the click to miss even though the button was
      // already reported visible. Dispatch the DOM click directly after the
      // visibility check so we still exercise Angular's real approval handler
      // and backend request without depending on pointer coordinates.
      card.approveButton.should('be.visible').trigger('click');

      cy.wait("@approveCall").then((interception) => {
        expect(interception.response?.statusCode).to.be.within(200, 299);
        card.status.should('contain.text', 'Approved');
      });

    });
  });

  it("Approve Requested Tv Show", () => {

    const username = `pendingTv${Date.now()}${Cypress._.random(1000, 9999)}`;
    const password = "password";

    cy.createUser(username, password, [{
      value: "requesttv",
      Enabled: "true",
    }]);
    cy.loginWithCreds(username, password);
    cy.requestAllTv(71712);
    cy.login();

    cy.intercept("GET", "**/v2/Requests/recentlyRequested").as("response");
    cy.intercept("POST", "**/v1/Request/tv/approve").as("approveCall");

    Page.visit();

    cy.wait("@response").then((_) => {

      const card = Page.recentlyRequested.getRequest("71712");
      card.status.should('contain.text', 'Pending');
      card.reveal();
      card.approveButton.should('be.visible').trigger('click');

      cy.wait("@approveCall").then((interception) => {
        expect(interception.response?.statusCode).to.be.within(200, 299);
        card.status.should('contain.text', 'Approved');
      });

    });
  });

});
