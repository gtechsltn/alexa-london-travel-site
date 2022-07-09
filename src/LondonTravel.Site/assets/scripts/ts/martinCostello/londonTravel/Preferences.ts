// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace martinCostello.londonTravel {

    /**
     * Represents the class for managing user preferences.
     */
    export class Preferences {

        private readonly clearButton: JQuery<Element>;
        private readonly container: JQuery<Element>;
        private readonly favoritesCount: JQuery<Element>;
        private readonly initialState: string;
        private readonly otherCount: JQuery<Element>;
        private readonly resetButton: JQuery<Element>;
        private readonly saveButton: JQuery<Element>;

        /**
         * Initializes a new instance of the martinCostello.londonTravel.Preferences class.
         * @constructor
         */
        public constructor() {

            this.container = $(".js-preferences-container");
            this.initialState = this.getCurrentState();

            this.clearButton = this.container.find(".js-preferences-clear");
            this.resetButton = this.container.find(".js-preferences-reset");
            this.saveButton = this.container.find(".js-preferences-save");

            this.favoritesCount = this.container.find(".js-favorites-count").removeClass("d-none");
            this.otherCount = this.container.find(".js-other-count").removeClass("d-none");

            // Treat entire div as a button
            this.container
                .find(".js-travel-line")
                .on("click", this.onLineClicked);

            // Show the reset button to restore initial preferences' state
            this.resetButton
                .on("click", this.resetFavoriteLines)
                .addClass("disabled")
                .prop("disabled", true)
                .removeClass("d-none");

            // Disable the clear button if everything is currently deselected
            if (this.initialState === "") {
                this.clearButton
                    .addClass("disabled")
                    .prop("disabled", true);
            }

            // Add handler for clearing the selections and show the button
            this.clearButton
                .on("click", this.clearFavoriteLines)
                .removeClass("d-none");

            // Disable the save button the state is different
            this.saveButton
                .addClass("disabled")
                .prop("disabled", true);

            // Check the form's state whenever an option is changed
            this.container
                .find(".js-line-preference")
                .on("change", this.checkCurrentState);

            // Dismiss any visible success alerts
            setTimeout(() => {
                $(".alert-success").slideUp(1000);
            }, 3000);
        }

        /**
         * Checks the current state of the preferences form.
         */
        private checkCurrentState = (): void => {

            const currentState: string = this.getCurrentState();
            const isDirty: boolean = this.initialState !== currentState;

            this.setButtonState(this.resetButton, isDirty);
            this.setButtonState(this.saveButton, isDirty);
            this.setButtonState(this.clearButton, currentState !== "");

            if (this.otherCount.length > 0) {
                const total: number = this.getAllCheckboxes().length;
                const favorites: number = this.getSelectedCheckboxes().length;
                const others: number = total - favorites;

                this.favoritesCount.text(`(${favorites.toString(10)})`);
                this.otherCount.text(`(${others.toString(10)})`);
            }
        }

        /**
         * Clears all of the selected lines.
         */
        private clearFavoriteLines = (): void => {

            this.getSelectedCheckboxes()
                .prop("checked", false);

            this.checkCurrentState();
        }

        /**
         * Gets all of the checkboxes in the preferences control.
         * @returns {JQuery<Element>} - The jQuery element containing all the checkboxes.
         */
        private getAllCheckboxes = (): JQuery<Element> => {
            return this.container.find("input[type='checkbox']");
        }

        /**
         * Gets the current state of the preferences.
         * @returns {string} - The current state of the preferences.
         */
        private getCurrentState = (): string => {

            const ids: string[] = [];

            this.getSelectedCheckboxes()
                .each((_: number, elem: Element) => {
                    const element = $(elem);
                    ids.push(element.attr("data-line-id"));
                });

            ids.sort();

            return ids.join();
        }

        /**
         * Gets the selected checkboxes in the preferences control.
         * @returns {JQuery<Element>} - The jQuery element containing the selected checkboxes.
         */
        private getSelectedCheckboxes = (): JQuery<Element> => {
            return this.container.find("input[type='checkbox']:checked");
        }

        /**
         * Handles a line being clicked.
         * @param {JQuery.TriggeredEvent} e - The event arguments.
         */
        private onLineClicked = (e: JQuery.TriggeredEvent): void => {

            const element = $(e.target);
            const checkbox = element.find("input[type='checkbox']");
            checkbox.prop("checked", !checkbox.prop("checked"));

            this.checkCurrentState();
        }

        /**
         * Resets the selected lines.
         */
        private resetFavoriteLines = (): void => {

            const ids: string[] = this.initialState.split(",");

            this.getAllCheckboxes()
                .each((_: number, elem: Element) => {
                    const element = $(elem);
                    const id: string = element.attr("data-line-id");
                    element.prop("checked", ids.indexOf(id) > -1);
                });

            this.checkCurrentState();
        }

        /**
         * Sets the state of the specified button to be enabled or not.
         * @param {JQuery<Element>} button - The button to enable or disable.
         * @param {boolean} enabled - Whether the button is enabled, or not.
         */
        private setButtonState = (button: JQuery<Element>, enabled: boolean): void => {
            if (enabled === true) {
                button.prop("disabled", false).removeClass("disabled");
            }
            else {
                button.prop("disabled", true).addClass("disabled");
            }
        }
    }
}
(() => {
    const _ = new martinCostello.londonTravel.Preferences();
})();
