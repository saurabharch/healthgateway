import { ActionTree, Commit } from "vuex";

import { IConfigService } from "@/services/interfaces";
import SERVICE_IDENTIFIER from "@/constants/serviceIdentifiers";
import container from "@/inversify.config";
import { RootState, ConfigState } from "@/models/storeState";

function handleError(commit: Commit, error: Error) {
  console.log("ERROR:" + error);
  commit("configurationError");
}

export const actions: ActionTree<ConfigState, RootState> = {
  initialize({ commit }): any {
    console.log("Initializing the config store...");
    const configService: IConfigService = container.get(
      SERVICE_IDENTIFIER.ConfigService
    );

    return new Promise((resolve, reject) => {
      configService
        .getConfiguration()
        .then(value => {
          console.log("Configuration: ", value);
          commit("configurationLoaded", value);
          resolve();
        })
        .catch(error => {
          handleError(commit, error);
          reject(error);
        })
        .finally(() => {
          console.log("Finished initialization");
        });
    });
  }
};
