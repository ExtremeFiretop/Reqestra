import { DiscoverType } from "@/integration/page-objects/shared/DiscoverCard";
import { discoverPage as Page } from "@/integration/page-objects";
import popularMovies from "@fixtures/discover/popularMovies.json";
import popularTv from "@fixtures/discover/popularTv.json";

const initialDiscoverLoad = 20;

// These tests exercise Reqestra card/request state, not the contents or
// ordering of TMDB's live Popular feed. Use the repository fixture so a newly
// listed movie with sparse external metadata cannot change the card's loading
// path and make the state assertions nondeterministic.
//
// The fixture contains fewer than the carousel's 20-item initial target.
// Preserve the production pagination contract by returning a distinct second
// page when the carousel asks to top up the missing cards.
const interceptPopularMovie = (index: number, overrides: Partial<(typeof popularMovies)[number]>) => {
  const body = popularMovies.map((movie) => ({ ...movie }));
  Object.assign(body[index], overrides);

  const amountToTopUp = Math.max(0, initialDiscoverLoad - body.length);
  const topUpMovies = popularMovies.slice(0, amountToTopUp).map((movie, topUpIndex) => {
    const id = 9_000_000 + topUpIndex;
    return {
      ...movie,
      id,
      theMovieDbId: id.toString(),
      imdbId: `tt9${(topUpIndex + 1).toString().padStart(7, "0")}`,
      title: `Top-up Movie ${topUpIndex + 1}`,
      originalTitle: `Top-up Movie ${topUpIndex + 1}`,
    };
  });

  cy.intercept(
    "GET",
    `**/search/Movie/Popular/0/${initialDiscoverLoad}`,
    { body },
  ).as("cardsResponse");

  if (amountToTopUp > 0) {
    cy.intercept(
      "GET",
      `**/search/Movie/Popular/${initialDiscoverLoad}/${amountToTopUp}`,
      { body: topUpMovies },
    ).as("cardsTopUpResponse");
  }
};

// Keep TV request-state tests independent from TMDB's live Popular ordering.
// The checked-in fixture gives us stable cards while the explicit overrides
// model the state each test is exercising. Do not let the carousel's top-up
// request fall through to the live provider either; an empty second page is
// sufficient for these card-state tests and avoids duplicate fixture IDs.
const interceptPopularTv = (index: number, overrides: Partial<(typeof popularTv)[number]>) => {
  const body = popularTv.map((show) => ({ ...show }));
  Object.assign(body[index], overrides);

  cy.intercept(
    "GET",
    `**/search/Tv/popular/0/${initialDiscoverLoad}`,
    { body },
  ).as("cardsResponse");

  cy.intercept(
    "GET",
    `**/search/Tv/popular/${initialDiscoverLoad}/**`,
    { body: [] },
  ).as("cardsTopUpResponseTv");
};

describe("Discover Cards Requests Tests", () => {
  beforeEach(() => {
    cy.login();
  });

  it("Tops up a short popular movie response from the next source offset", () => {
    interceptPopularMovie(0, {
      available: false,
      approved: false,
      requested: false,
    });

    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "2");
    });

    Page.visit();

    cy.wait("@cardsResponse").then((initial) => {
      expect(initial.response!.body).to.have.length(popularMovies.length);
    });

    cy.wait("@cardsTopUpResponse").then((topUp) => {
      expect(topUp.request.url).to.match(/\/search\/Movie\/Popular\/20\/4(?:\?.*)?$/i);
      expect(topUp.response!.body).to.have.length(4);

      const ids = topUp.response!.body.map((movie: (typeof popularMovies)[number]) => movie.id);
      expect(new Set(ids).size).to.equal(ids.length);
      expect(ids.every((id: number) => !popularMovies.some((movie) => movie.id === id))).to.be.true;
    });
  });

  it("Not requested movie allows admin to request", () => {
    interceptPopularMovie(0, {
      available: false,
      approved: false,
      requested: false,
    });

    cy.intercept("POST", "**/Request/Movie", {
      result: true,
      isError: false,
      errorMessage: null,
    }).as("movieRequest");

    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "2");
    });

    Page.visit();

    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body;
      var expectedId = body[0].id;
      var title = body[0].title;

      const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
      card.verifyTitle(title);
      card.requestButton.should("exist");
      // Not visible until hover
      card.requestButton.should("not.be.visible");
      card.topLevelCard.realHover();

      card.requestButton.should("be.visible");
      card.requestButton.click();

      Page.adminOptionsDialog.isOpen();
      Page.adminOptionsDialog.requestButton.click();

      cy.wait("@movieRequest");

      cy.verifyNotification("has been added successfully");

      // Assert the positive "requested" state first: these retry until the card
      // has re-rendered, which is also what removes the request button. Checking
      // button removal last avoids a race where the just-clicked (still focused)
      // button lingers in the DOM for a beat after the state change.
      card.availabilityText.should("have.text", "Pending");
      card.statusClass.should("have.class", "requested");
      card.requestButton.should("not.exist");
    });
  });

  it("Not requested movie allows non-admin to request", () => {
    cy.generateUniqueId().then((id) => {
      cy.login();
      const roles = [];
      roles.push({ value: "RequestMovie", enabled: true });
      cy.createUser(id, "a", roles).then(() => {
        cy.removeLogin();
        cy.loginWithCreds(id, "a");

        interceptPopularMovie(6, {
          available: false,
          approved: false,
          requested: false,
        });

        cy.intercept("POST", "**/Request/Movie", {
          result: true,
          isError: false,
          errorMessage: null,
        }).as("movieRequest");

        cy.then(() => {
          window.localStorage.setItem("DiscoverOptions2", "2");
        });

        Page.visit();

        cy.wait("@cardsResponse").then((res) => {
          const body = res.response!.body
          var expectedId = body[6].id;
          var title = body[6].title;

          const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
          card.verifyTitle(title);
          card.requestButton.should("exist");
          // Not visible until hover
          card.requestButton.should("not.be.visible");
          card.topLevelCard.realHover();

          card.requestButton.should("be.visible");
          card.requestButton.click();

          cy.wait("@movieRequest");

          cy.verifyNotification("has been added successfully");

          // Assert the positive "requested" state first (these retry until the
          // card re-renders, which is what removes the button); check button
          // removal last to avoid a race with the just-clicked focused button.
          card.availabilityText.should("have.text", "Pending");
          card.statusClass.should("have.class", "requested");
          card.requestButton.should("not.exist");
        });
      });
    });
  });

  it("Available movie does not allow us to request", () => {
    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "2");
    });
    interceptPopularMovie(1, {
      available: true,
      approved: false,
      requested: false,
    });

    Page.visit();

    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body
      var expectedId = body[1].id;
      var title = body[1].title;

      const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
      card.verifyTitle(title);
      card.topLevelCard.realHover();

      card.requestButton.should("not.exist");
      card.availabilityText.should("have.text", "Available");
      card.statusClass.should("have.class", "available");
    });
  });

  it("Requested movie does not allow us to request", () => {
    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "2");
    });
    interceptPopularMovie(1, {
      available: false,
      approved: false,
      requested: true,
    });

    Page.visit();

    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body
      var expectedId = body[1].id;
      var title = body[1].title;

      const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
      card.title.realHover();

      card.verifyTitle(title);
      card.requestButton.should("not.exist");
      card.availabilityText.should("have.text", "Pending");
      card.statusClass.should("have.class", "requested");
    });
  });

  it("Approved movie does not allow us to request", () => {
    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "2");
    });
    interceptPopularMovie(1, {
      available: false,
      approved: true,
      requested: true,
    });

    Page.visit();

    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body
      var expectedId = body[1].id;
      var title = body[1].title;

      const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
      card.title.realHover();

      card.verifyTitle(title);
      card.requestButton.should("not.exist");
      card.availabilityText.should("have.text", "Approved");
      card.statusClass.should("have.class", "approved");
    });
  });

  it("Available TV does not allow us to request", () => {
    interceptPopularTv(1, {
      available: false,
      approved: false,
      requested: false,
      fullyAvailable: true,
      partlyAvailable: false,
    });
    cy.intercept("GET", "**/search/Tv/moviedb/**", (req) => {
      req.reply((res2) => {
        const body = res2.body;
        body.fullyAvailable = true;
        res2.send(body);
      });
    }).as("movieDbResponse");
    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "3");
    });

    Page.visit();

    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body
      var expectedId = body[1].id;
      var title = body[1].title;

      const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
      card.title.realHover();

      card.verifyTitle(title);
      card.requestButton.should("not.exist");
      card.availabilityText.should("have.text", "Available");
      card.statusClass.should("have.class", "available");
    });
  });

  it("Available TV (From Details Call) does not allow us to request", () => {
    interceptPopularTv(0, {
      available: false,
      approved: false,
      requested: false,
      fullyAvailable: false,
      partlyAvailable: false,
    });
    cy.intercept("GET", "**/search/Tv/moviedb/88396", (req) => {
      req.reply((res2) => {
        const body = res2.body;
        body.fullyAvailable = true;
        res2.send(body);
      });
    }).as("movieDbResponse");
    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "3");
    });

    Page.visit();

    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body;
      var expectedId = "88396";

      var title = body[0].title;


      cy.wait("@movieDbResponse")

      const card = Page.popularCarousel.getCard(expectedId, true, DiscoverType.Popular);
      card.title.realHover();

      card.verifyTitle(title);
      card.requestButton.should("not.exist");
      card.availabilityText.should("have.text", "Available");
      card.statusClass.should("have.class", "available");
    });
  });

  it("Not available TV allow admin to request", () => {
    const tvIndex = 4;
    interceptPopularTv(tvIndex, {
      available: false,
      approved: false,
      requested: false,
      fullyAvailable: false,
      partlyAvailable: false,
      requestId: 0,
    });
    cy.intercept("GET", "**/search/Tv/**").as("otherResponses");
    cy.intercept("POST", "**/Requests/TV/", {
      result: true,
      isError: false,
      errorMessage: null,
    }).as("tvRequest");
    cy.then(() => {
      window.localStorage.setItem("DiscoverOptions2", "3");
    });

    Page.visit();

    cy.wait("@otherResponses");
    cy.wait("@cardsResponse").then((res) => {
      const body = res.response!.body
      var expectedId = body[tvIndex].id;
      var title = body[tvIndex].title;

      const card = Page.popularCarousel.getCard(expectedId, false, DiscoverType.Popular);
      // The card resolves its availability via an async detail lookup and only
      // then renders the request button; a deterministic wait is not reliable
      // here (the button only renders once the carousel settles and the card is
      // hovered), so allow that async work a beat before hovering.
      cy.wait(3000);
      card.title.realHover();

      cy.waitUntil(() => {
        return card.requestButton.should("be.visible");
      });

      card.verifyTitle(title);
      card.requestButton.should("be.visible");
      card.requestButton.click();
      const modal = card.episodeRequestModal;

      modal.latestSeasonButton.click();

      Page.adminOptionsDialog.isOpen();
      Page.adminOptionsDialog.requestButton.click();

      cy.wait("@tvRequest");

      cy.verifyNotification("has been added successfully");
    });
  });

  it("Not available TV allow non-admin to request", () => {
    cy.generateUniqueId().then((id) => {
      cy.login();
      const roles = [];
      roles.push({ value: "RequestTv", enabled: true });
      cy.createUser(id, "a", roles).then(() => {
        cy.removeLogin();
        cy.loginWithCreds(id, "a");

        const tvIndex = 6;
        interceptPopularTv(tvIndex, {
          available: false,
          approved: false,
          requested: false,
          fullyAvailable: false,
          partlyAvailable: false,
          requestId: 0,
        });
        cy.intercept("GET", "**/search/Tv/**").as("otherResponses");
        cy.intercept("POST", "**/Requests/TV/", {
          result: true,
          isError: false,
          errorMessage: null,
        }).as("tvRequest");
        cy.then(() => {
          window.localStorage.setItem("DiscoverOptions2", "3");
        });

        Page.visit();

        cy.wait("@otherResponses");
        cy.wait("@cardsResponse").then((res) => {
          const body = res.response!.body
          var expectedId = body[tvIndex].id;
          var title = body[tvIndex].title;

          const card = Page.popularCarousel.getCard(expectedId, false, DiscoverType.Popular);
          // The card resolves its availability via an async detail lookup and
          // only then renders the request button; a deterministic wait is not
          // reliable here (the button only renders once the carousel settles and
          // the card is hovered), so allow that async work a beat before hovering.
          cy.wait(3000);
          card.title.realHover();

          cy.waitUntil(() => {
            return card.requestButton.should("be.visible");
          });

          card.verifyTitle(title);
          card.requestButton.should("be.visible");
          card.requestButton.click();
          const modal = card.episodeRequestModal;

          modal.latestSeasonButton.click();

          cy.wait("@tvRequest");

          cy.verifyNotification("has been added successfully");
        });
      });
    });
  });
});
