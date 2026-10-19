import { discoverPage as Page } from "@/integration/page-objects";

describe("Discover Cards Tests", () => {
  beforeEach(() => {
    cy.login();
  });

  it("Explains the difference between Popular and Trending in the section headings", () => {
    Page.visit();

    cy.contains("h2", "Popular (Most Watched)").should("exist");
    cy.contains("h2", "Trending (This Week)").should("exist");
  });

  it("Seasonal loads while hidden and becomes visible when movies are returned", () => {
    cy.intercept("GET", "**/search/Movie/seasonal/**", { fixture: "discover/popularMovies.json" }).as("seasonalMovies");

    Page.visit();

    cy.wait("@seasonalMovies");
    cy.get("#seasonal-section").should("be.visible");
    cy.get("#seasonal-section carousel-list").should("exist");
  });

  it("Seasonal stays hidden when no movies are returned", () => {
    cy.intercept("GET", "**/search/Movie/seasonal/**", []).as("seasonalMovies");

    Page.visit();

    cy.wait("@seasonalMovies");
    cy.get("#seasonal-section").should("not.be.visible");
  });

  it("Keeps desktop carousel slots from stretching when a feed is sparse", () => {
    cy.viewport(1920, 1080);
    window.localStorage.setItem("DiscoverOptions2", "2");
    cy.intercept("GET", "**/search/Movie/Popular/**").as("moviePopular");

    Page.visit();
    cy.wait("@moviePopular");

    cy.get("#popularMovie-button")
      .closest("carousel-list")
      .find(".p-carousel-item")
      .first()
      .should("have.css", "flex-grow", "0")
      .and("have.css", "flex-basis", "200px");
  });

  it("Popular combined should load movies and TV", () => {
    cy.intercept("GET", "**/search/Movie/Popular/**").as("moviePopular");
    cy.intercept("GET", "**/search/Tv/popular/**").as("tvPopular");
    Page.visit();

    cy.wait("@moviePopular");
    cy.wait("@tvPopular");
  });

  it("Popular Movie should load movies", () => {
    cy.intercept("GET", "**/search/Movie/Popular/**").as("moviePopular");
    Page.visit();
    Page.popularCarousel.movieButton.click();

    cy.wait("@moviePopular");
  });

  it("Popular TV should load TV", () => {
    cy.intercept("GET", "**/search/Tv/popular/**").as("tvPopular");
    Page.visit();
    Page.popularCarousel.tvButton.click();

    cy.wait("@tvPopular");
  });

  it("Popular Moives selected when set in localstorage", () => {
    window.localStorage.setItem("DiscoverOptions2", "2");
    cy.intercept("GET", "**/search/Movie/Popular/**").as("moviePopular");
    Page.visit();
    Page.popularCarousel.movieButton
      .parent()
      .should("have.class", "filter-active");

    cy.wait("@moviePopular");
  });

  it("Popular Tv selected when set in localstorage", () => {
    window.localStorage.setItem("DiscoverOptions2", "3");
    cy.intercept("GET", "**/search/Tv/popular/**").as("tvPopular");
    Page.visit();
    Page.popularCarousel.tvButton
      .parent()
      .should("have.class", "filter-active");

    cy.wait("@tvPopular");
  });

  it("Popular Combined selected when set in localstorage", () => {
    window.localStorage.setItem("DiscoverOptions2", "1");
    cy.intercept("GET", "**/search/Movie/Popular/**").as("moviePopular");
    cy.intercept("GET", "**/search/Tv/popular/**").as("tvPopular");
    Page.visit();
    Page.popularCarousel.combinedButton
      .parent()
      .should("have.class", "filter-active");

    cy.wait("@moviePopular");
    cy.wait("@tvPopular");
  });
});
