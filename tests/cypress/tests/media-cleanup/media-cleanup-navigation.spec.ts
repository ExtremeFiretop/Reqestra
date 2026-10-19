describe("Media Cleanup descriptions and details links", () => {
  beforeEach(() => {
    cy.login();

    const overview = {
      settings: {
        ownRequestRemoval: 1,
        communityCleanup: 0,
        minimumDeleteVotes: 2,
        requiredVoteMargin: 1,
        votingPeriodDays: 7,
        gracePeriodDays: 2,
        minimumMediaAgeDays: 0,
        anyKeepVotePreventsRemoval: false,
        requesterCanVeto: false,
        restrictNominationsToOwnRequests: false,
        deleteFiles: false,
        addImportExclusion: false,
        notifyManagersOnPendingApproval: false,
        notifyVotersOnPendingVotes: false,
        enabled: true,
      },
      canRequestRemoval: true,
      canDeleteOwnMedia: false,
      canVote: false,
      canManage: false,
      items: [
        {
          requestType: 1,
          requestId: 42,
          mediaId: 550,
          title: "Expandable Movie",
          posterPath: "/poster.jpg",
          overview: "This is a deliberately long movie description used to verify that Media Cleanup keeps its compact preview by default while still allowing the user to expand the full synopsis in place without leaving the cleanup page. The description continues with enough text to clearly exceed the normal four-line catalog preview and confirm that the Show more control changes to Show less when expanded.",
          releaseDate: "1999-10-15T00:00:00Z",
          requestedBy: "Test User",
          ownedByCurrentUser: true,
          isCleanupSteward: false,
          canRequestOwnRemoval: true,
          canNominate: false,
          canVote: false,
          canManage: false,
          canCancel: false,
          communityAgeEligible: true,
          lastPlayedKnown: false,
          sizeOnDisk: 0,
        },
        {
          requestType: 0,
          requestId: 43,
          mediaId: 1399,
          title: "Linked TV Show",
          posterPath: "/tv-poster.jpg",
          overview: "A TV description.",
          releaseDate: "2011-04-17T00:00:00Z",
          requestedBy: "Test User",
          ownedByCurrentUser: true,
          isCleanupSteward: false,
          canRequestOwnRemoval: true,
          canNominate: false,
          canVote: false,
          canManage: false,
          canCancel: false,
          communityAgeEligible: true,
          lastPlayedKnown: false,
          sizeOnDisk: 0,
        },
      ],
    };

    cy.intercept("GET", "**/api/v1/MediaCleanup?includeMetrics=false&includeLastPlayed=false", overview).as("cleanupOverview");
    cy.intercept("GET", "**/api/v1/MediaCleanup?includeMetrics=true&includeLastPlayed=false", overview).as("cleanupMetrics");
    cy.intercept("GET", "**/api/v1/MediaCleanup?includeMetrics=false&includeLastPlayed=true", overview).as("cleanupPlayback");

    cy.visit("/cleanup");
    cy.wait("@cleanupOverview");
  });

  it("expands and collapses a catalog description in place", () => {
    cy.contains(".cleanup-card", "Expandable Movie").within(() => {
      cy.get(".media-overview-catalog").should("not.have.class", "media-overview-expanded");
      cy.contains(".overview-toggle-label", "Show more").click();
      cy.get(".media-overview-catalog").should("have.class", "media-overview-expanded");
      cy.contains(".overview-toggle-label", "Show less").click();
      cy.get(".media-overview-catalog").should("not.have.class", "media-overview-expanded");
    });
  });

  it("links movie and TV titles directly to their details pages", () => {
    cy.contains(".cleanup-card h2 a", "Expandable Movie")
      .should("have.attr", "href")
      .and("include", "/details/movie/550");

    cy.contains(".cleanup-card h2 a", "Linked TV Show")
      .should("have.attr", "href")
      .and("include", "/details/tv/1399");
  });
});
