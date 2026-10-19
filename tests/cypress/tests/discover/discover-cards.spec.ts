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

  it("Refills sparse filtered movie feeds while keeping desktop spacing compact", () => {
    cy.viewport(3840, 2160);
    window.localStorage.setItem("DiscoverOptions2", "2");

    let popularRequest = 0;
    cy.fixture("discover/popularMovies.json").then((movies) => {
      const makeBatch = (start: number, count: number) =>
        Array.from({ length: count }, (_, index) => {
          const source = movies[(start + index) % movies.length];
          return {
            ...source,
            id: 90000 + start + index,
            title: `${source.title} ${start + index}`,
          };
        });

      const batches = [makeBatch(0, 10), makeBatch(10, 7), makeBatch(17, 3)];

      cy.intercept("GET", "**/search/Movie/Popular/**", (req) => {
        const body = batches[popularRequest] ?? [];
        popularRequest++;
        req.reply({ statusCode: 200, body });
      }).as("moviePopular");

      Page.visit();
      cy.wait("@moviePopular");
      cy.wait("@moviePopular");
      cy.wait("@moviePopular");
    });

    cy.get("#popularMovie-button")
      .closest("carousel-list")
      .find(".p-carousel-item")
      .first()
      .should(($item) => {
        expect($item[0].getBoundingClientRect().width).to.be.lessThan(260);
      });

    cy.get("#popularMovie-button")
      .closest("carousel-list")
      .find("discover-card .card-wrapper")
      .should("have.length", 20)
      .then(($cards) => {
        const first = $cards[0].getBoundingClientRect();
        const second = $cards[1].getBoundingClientRect();
        expect(second.left - first.right).to.be.lessThan(50);
      });
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
