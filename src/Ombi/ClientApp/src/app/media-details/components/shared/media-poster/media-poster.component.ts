import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ImageComponent } from "../../../../components";
import { RequestType } from "../../../../interfaces";

@Component({
    standalone: true,
    selector: "media-poster",
    templateUrl: "./media-poster.component.html",
    imports: [
        CommonModule,
        ImageComponent
    ]
})
export class MediaPosterComponent {
    
    @Input() posterPath: string;
    @Input() type: RequestType;
}
