import { Component, ViewChild, Inject, input, output, signal, computed, inject, ChangeDetectionStrategy, ElementRef, OnDestroy } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatButtonToggleModule, MatButtonToggleChange } from '@angular/material/button-toggle';
import { TranslateModule } from "@ngx-translate/core";
import { CarouselModule, Carousel } from 'primeng/carousel';
import { SkeletonModule } from 'primeng/skeleton';

import { DiscoverOption, IDiscoverCardResult } from "../../interfaces";
import { ISearchMovieResult, ISearchTvResult, RequestType } from "../../../interfaces";
import { SearchV2Service } from "../../../services";
import { StorageService } from "../../../shared/storage/storage-service";
import { FeaturesFacade } from "../../../state/features/features.facade";
import { APP_BASE_HREF } from "@angular/common";
import { DiscoverCardComponent } from "../card/discover-card.component";

export enum DiscoverType {
    Upcoming,
    Trending,
    Popular,
    RecentlyRequested,
    Seasonal,
}

@Component({
    standalone: true,
    selector: "carousel-list",
    templateUrl: "./carousel-list.component.html",
    styleUrls: ["./carousel-list.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatButtonToggleModule,
        TranslateModule,
        CarouselModule,
        SkeletonModule,
        DiscoverCardComponent
    ]
})
export class CarouselListComponent implements OnDestroy {
    // Inputs using new input() function
    public discoverType = input.required<DiscoverType>();
    public id = input.required<string>();
    public isAdmin = input<boolean>(false);
    
    // Output using new output() function  
    public movieCount = output<number>();
    
    @ViewChild('carousel', {static: false}) carousel: Carousel;
    private carouselRootRef?: ElementRef<HTMLElement>;

    @ViewChild('carouselRoot', { static: false })
    set carouselRoot(value: ElementRef<HTMLElement> | undefined) {
        this.carouselRootRef = value;

        // The carousel lives inside an @defer block, so it may not exist when the
        // results are first modeled. A ViewChild setter runs when that deferred
        // view actually materializes, which gives us a reliable point to attach
        // the observer and refine the initial viewport-based sizing.
        if (!value) {
            return;
        }

        this.resizeObserver?.disconnect();
        this.resizeObserver = undefined;
        this.initResizeObserver();
        setTimeout(() => this.recalculateCarouselDimensions(), 0);
    }

    // Services using inject() function
    private searchService = inject(SearchV2Service);
    private storageService = inject(StorageService);
    private featureFacade = inject(FeaturesFacade);
    private baseUrl = inject(APP_BASE_HREF);

    // Public constants
    public DiscoverOption = DiscoverOption;
    public RequestType = RequestType;
    public DiscoverType = DiscoverType;
    
    // State using signals
    public discoverOptions = signal<DiscoverOption>(DiscoverOption.Combined);
    public discoverResults = signal<IDiscoverCardResult[]>([]);
    public movies = signal<ISearchMovieResult[]>([]);
    public tvShows = signal<ISearchTvResult[]>([]);
    public loadingFlag = signal<boolean>(false);
    public is4kEnabled = signal<boolean>(false);
    public numVisible = signal<number>(10);
    public numScroll = signal<number>(9);
    
    // Computed properties
    public hasResults = computed(() => this.discoverResults().length > 0);
    public totalResults = computed(() => this.discoverResults().length);

    get mediaTypeStorageKey() {
        return "DiscoverOptions" + this.discoverType().toString();
    };
    
    private amountToLoad = 20;
    private currentlyLoaded = 0;
    private loadVersion = 0;
    private resizeObserver?: ResizeObserver;

    constructor() {
        Carousel.prototype.onTouchMove = () => { };

        // Give PrimeNG a sensible slot count before the deferred carousel is
        // rendered. Without this, numVisible starts at 10 on every viewport; on
        // phones that briefly creates ten tiny slots and can leave the carousel
        // in that state if the deferred ViewChild has not appeared yet.
        if (typeof window !== 'undefined') {
            this.updateCarouselDimensions(window.innerWidth);
        }
    }

    public async ngOnInit() {
        // Initialize 4K feature flag
        this.is4kEnabled.set(this.featureFacade.is4kEnabled());
        this.currentlyLoaded = 0;
        
        // Load saved discover options from storage
        const localDiscoverOptions = +this.storageService.get(this.mediaTypeStorageKey);
        if (localDiscoverOptions) {
            this.discoverOptions.set(DiscoverOption[DiscoverOption[localDiscoverOptions]]);
        }

        // Load the first batch and render it immediately.
        const loadVersion = ++this.loadVersion;
        await this.loadData(false);

        // If filtering leaves the carousel short, top it up in the background.
        // This deliberately caps the automatic initial load at two batches while
        // avoiding a second expensive TV enrichment pass on the critical render path.
        if (this.discoverResults().length < this.amountToLoad) {
            void this.topUpInitialCarousel(loadVersion);
        }
    }

    public async toggleChanged(event: MatButtonToggleChange) {
        await this.switchDiscoverMode(event.value);
    }

    public async newPage() {
        // Note this is using the internal carousel APIs
        // https://github.com/primefaces/primeng/blob/master/src/app/components/carousel/carousel.ts
        if (!this.carousel?.page) {
            return;
        }

        var end = this.carousel.page >= (this.carousel.totalDots() - 2) || this.carousel.totalDots() === 1;
        if (end) {
            var offset = this.currentlyLoaded;
            var moviePromise: Promise<void>;
            var tvPromise: Promise<void>;
            switch (+this.discoverOptions()) {
                case DiscoverOption.Combined:
                    moviePromise = this.loadMovies(offset);
                    tvPromise = this.loadTv(offset);
                    break;
                case DiscoverOption.Movie:
                    moviePromise = this.loadMovies(offset);
                    break;
                case DiscoverOption.Tv:
                    tvPromise = this.loadTv(offset);
                    break;
            }
            await Promise.all([moviePromise, tvPromise].filter(Boolean));
            this.currentlyLoaded += this.amountToLoad;

            this.createModel();
        }
    }

    private async loadData(clearExisting: boolean = true) {
        var offset = this.currentlyLoaded;
        var moviePromise: Promise<void>;
        var tvPromise: Promise<void>;
        switch (+this.discoverOptions()) {
            case DiscoverOption.Combined:
                moviePromise = this.loadMovies(offset);
                tvPromise = this.loadTv(offset);
                break;
            case DiscoverOption.Movie:
                moviePromise = this.loadMovies(offset);
                break;
            case DiscoverOption.Tv:
                tvPromise = this.loadTv(offset);
                break;
        }
        await Promise.all([moviePromise, tvPromise].filter(Boolean));
        this.currentlyLoaded += this.amountToLoad;
        this.createInitialModel(clearExisting);
    }

    private async topUpInitialCarousel(loadVersion: number) {
        // Yield so Angular can paint the first batch before starting another request.
        await new Promise<void>(resolve => setTimeout(resolve, 0));

        if (loadVersion !== this.loadVersion || this.discoverResults().length >= this.amountToLoad) {
            return;
        }

        const offset = this.currentlyLoaded;
        const discoverOption = +this.discoverOptions();
        const amountToTopUp = this.amountToLoad - this.discoverResults().length;

        try {
            let movieResults: ISearchMovieResult[] | undefined;
            let tvResults: ISearchTvResult[] | undefined;

            switch (discoverOption) {
                case DiscoverOption.Combined:
                    [movieResults, tvResults] = await Promise.all([
                        this.fetchMovies(offset, amountToTopUp),
                        this.fetchTv(offset, amountToTopUp)
                    ]);
                    break;
                case DiscoverOption.Movie:
                    movieResults = await this.fetchMovies(offset, amountToTopUp);
                    break;
                case DiscoverOption.Tv:
                    tvResults = await this.fetchTv(offset, amountToTopUp);
                    break;
            }

            // The user may have changed media mode while the background request was running.
            if (loadVersion !== this.loadVersion || discoverOption !== +this.discoverOptions()) {
                return;
            }

            if (movieResults) {
                this.movies.set(movieResults);
                this.movieCount.emit(movieResults.length);
            }
            if (tvResults) {
                this.tvShows.set(tvResults);
            }

            this.currentlyLoaded += amountToTopUp;
            this.createInitialModel(false);
        } catch (error) {
            console.warn('Discover carousel background top-up failed', error);
        }
    }

    private async switchDiscoverMode(newMode: DiscoverOption) {
        if (this.discoverOptions() === newMode) {
            return;
        }
        // Invalidate any deferred initial top-up from the previous mode.
        this.loadVersion++;
        this.loading();
        this.currentlyLoaded = 0;
        this.discoverOptions.set(+newMode);
        this.storageService.save(this.mediaTypeStorageKey, newMode.toString());
        await this.loadData();
        this.finishLoading();
    }

    private async loadMovies(offset?: number) {
        var loadOffset = offset ?? this.currentlyLoaded;
        this.movies.set(await this.fetchMovies(loadOffset));
        this.movieCount.emit(this.movies().length);
    }

    private async fetchMovies(loadOffset: number, amountToLoad: number = this.amountToLoad): Promise<ISearchMovieResult[]> {
        switch (this.discoverType()) {
            case DiscoverType.Popular:
                return await this.searchService.popularMoviesByPage(loadOffset, amountToLoad);
            case DiscoverType.Trending:
                return await this.searchService.nowPlayingMoviesByPage(loadOffset, amountToLoad);
            case DiscoverType.Upcoming:
                return await this.searchService.upcomingMoviesByPage(loadOffset, amountToLoad);
            case DiscoverType.RecentlyRequested:
                return await this.searchService.recentlyRequestedMoviesByPage(loadOffset, amountToLoad);
            case DiscoverType.Seasonal:
                return await this.searchService.seasonalMoviesByPage(loadOffset, amountToLoad);
            default:
                return [];
        }
    }

    private async loadTv(offset?: number) {
        var loadOffset = offset ?? this.currentlyLoaded;
        this.tvShows.set(await this.fetchTv(loadOffset));
    }

    private async fetchTv(loadOffset: number, amountToLoad: number = this.amountToLoad): Promise<ISearchTvResult[]> {
        switch (this.discoverType()) {
            case DiscoverType.Popular:
                return await this.searchService.popularTvByPage(loadOffset, amountToLoad);
            case DiscoverType.Trending:
                return await this.searchService.trendingTvByPage(loadOffset, amountToLoad);
            case DiscoverType.Upcoming:
                return await this.searchService.anticipatedTvByPage(loadOffset, amountToLoad);
            case DiscoverType.RecentlyRequested:
                // TODO need to do some more mapping
                return [];
            default:
                return [];
        }
    }

    private createInitialModel(clearExisting: boolean = true) {
        if (clearExisting) {
            this.clear();
        }
        this.createModel();
    }

    private createModel() {
        const tempResults = <IDiscoverCardResult[]>[];

        switch (+this.discoverOptions()) {
            case DiscoverOption.Combined:
                tempResults.push(...this.mapMovieModel());
                tempResults.push(...this.mapTvModel());
                this.shuffle(tempResults);
                break;
            case DiscoverOption.Movie:
                tempResults.push(...this.mapMovieModel());
                break;
            case DiscoverOption.Tv:
                tempResults.push(...this.mapTvModel());
                break;
        }

        this.discoverResults.update(current => [...current, ...tempResults]);
        this.scheduleCarouselSizing();

        this.finishLoading();
    }

    private mapMovieModel(): IDiscoverCardResult[] {
        const tempResults = <IDiscoverCardResult[]>[];
        this.movies().forEach(m => {
            tempResults.push({
                available: m.available,
                posterPath: m.posterPath ? `https://image.tmdb.org/t/p/w500${m.posterPath}` : this.baseUrl + "/images/default_movie_poster.png",
                requested: m.requested,
                title: m.title,
                type: RequestType.movie,
                id: m.id,
                url: `http://www.imdb.com/title/${m.imdbId}/`,
                rating: m.voteAverage,
                overview: m.overview,
                approved: m.approved,
                imdbid: m.imdbId,
                denied: false,
                background: m.backdropPath
            });
        });
        return tempResults;
    }

    private mapTvModel(): IDiscoverCardResult[] {
        const tempResults = <IDiscoverCardResult[]>[];
        this.tvShows().forEach(m => {
            const poster = m.posterPath || m.backdropPath;
            tempResults.push({
                available: m.fullyAvailable || m.partlyAvailable,
                posterPath: poster ? `https://image.tmdb.org/t/p/w500${poster}` :  this.baseUrl + "/images/default_tv_poster.png",
                requested: m.requested,
                title: m.title,
                type: RequestType.tvShow,
                id: m.id,
                url: undefined,
                rating: +m.rating,
                overview: m.overview,
                approved: m.approved || m.partlyAvailable,
                imdbid: m.imdbId,
                denied: false,
                background: m.backdropPath
            });
        });
        return tempResults;
    }

    private scheduleCarouselSizing() {
        // The carousel is rendered by an @defer block after results arrive. Yield
        // once so the root exists before measuring its actual content width.
        setTimeout(() => {
            this.initResizeObserver();
            this.recalculateCarouselDimensions();
        }, 0);
    }

    private initResizeObserver() {
        const root = this.carouselRootRef?.nativeElement;
        if (!root || this.resizeObserver) {
            return;
        }

        this.resizeObserver = new ResizeObserver(() => this.recalculateCarouselDimensions());
        this.resizeObserver.observe(root);
    }

    private recalculateCarouselDimensions() {
        const root = this.carouselRootRef?.nativeElement;
        if (!root) {
            return;
        }

        // Use the actual PrimeNG viewport rather than the browser width so sidebar,
        // navigation buttons, and page padding are naturally accounted for.
        const itemsContent = root.querySelector<HTMLElement>('.p-carousel-items-content');
        const width = (itemsContent ?? root).clientWidth;
        if (!width) {
            return;
        }

        this.updateCarouselDimensions(width);
    }

    private updateCarouselDimensions(width: number) {
        if (!width) {
            return;
        }

        // Discover cards are capped at 200px with 4px host padding on each side.
        // Keep this target stable instead of measuring the discover-card host: the
        // host itself inherits PrimeNG's current slot width, so feeding that value
        // back into numVisible can create an unstable sizing loop.
        const targetCardWidth = 208;

        // Do not clamp this to the result count. PrimeNG derives slot width and
        // translation distance from numVisible; keeping the viewport capacity here
        // prevents sparse feeds from stretching their few cards across the row.
        const maxVisible = 17;
        const visible = Math.max(1, Math.min(maxVisible, Math.floor(width / targetCardWidth)));
        const scroll = visible <= 2 ? visible : visible - 1;

        this.numVisible.set(visible);
        this.numScroll.set(Math.max(1, scroll));
    }

    public ngOnDestroy(): void {
        this.resizeObserver?.disconnect();
    }

    private clear() {
        this.discoverResults.set([]);
    }

    private shuffle(discover: IDiscoverCardResult[]): IDiscoverCardResult[] {
        for (let i = discover.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [discover[i], discover[j]] = [discover[j], discover[i]];
        }
        return discover;
    }

    private loading() {
        this.loadingFlag.set(true);
    }

    private finishLoading() {
        this.loadingFlag.set(false);
    }


}
