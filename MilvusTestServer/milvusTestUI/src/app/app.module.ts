import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import {FormsModule, ReactiveFormsModule} from '@angular/forms'
import { AppComponent } from './app.component';
import { TimelineComponent } from './timeline/timeline.component';
import { MultiSelectModule } from 'primeng/multiselect';
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import { ExpressionsComponent } from './expressions/expressions.component';

@NgModule({
  declarations: [AppComponent, TimelineComponent, ExpressionsComponent],
  imports: [BrowserModule, BrowserAnimationsModule, HttpClientModule, FormsModule, ReactiveFormsModule, MultiSelectModule],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {}
