// timeline.component.ts
import {Component, Input, OnInit} from '@angular/core';

export interface TimelineStage {
  IsIndexed : boolean;
  IsLoaded: boolean;
  HasData : boolean;
}

@Component({
  selector: 'app-timeline',
  templateUrl: './timeline.component.html',
  styleUrls: ['./timeline.component.scss'],
})
export class TimelineComponent implements OnInit{
  @Input() stages: any = {
    IsIndexed : false,
    IsLoaded : false,
    HasData : false,
  };

  objectKeys = Object.keys;
  constructor() {
  }

  ngOnInit()
  {
  }

  ngOnChanges() {
    // this.updateStages();
  }

  // Updates stages based on the current status
  // updateStages() {
  //   const statusIndex = this.stages.findIndex(stage => stage.label === this.status);
  //   this.stages.forEach((stage, index) => {
  //     stage.completed = index <= statusIndex;
  //   });
  // }
}
