import { ErrorSourceType, ErrorType } from "@/constants/errorType";
import { ResultError } from "@/models/errors";
import PatientData, {
    PatientDataFile,
    PatientDataType,
} from "@/models/patientData";
import { LoadStatus } from "@/models/storeOperations";
import container from "@/plugins/container";
import { SERVICE_IDENTIFIER } from "@/plugins/inversify";
import { ILogger, IPatientDataService } from "@/services/interfaces";
import { PatientDataActions } from "@/store/modules/patientData/types";
import {
    getPatientDataFileState,
    getPatientDataRecordState,
} from "@/store/modules/patientData/utils";

export const actions: PatientDataActions = {
    retrievePatientDataFile(
        context,
        params: { hdid: string; fileId: string }
    ): Promise<PatientDataFile> {
        const logger = container.get<ILogger>(SERVICE_IDENTIFIER.Logger);
        const patientDataService = container.get<IPatientDataService>(
            SERVICE_IDENTIFIER.PatientDataService
        );

        return new Promise((resolve, reject) => {
            const currentState = getPatientDataFileState(
                context.state,
                params.fileId
            );
            if (
                currentState.status === LoadStatus.LOADED &&
                currentState.data !== undefined
            ) {
                logger.debug("Patient data file found stored, not querying!");
                const patientDataFile: PatientDataFile = currentState.data;
                resolve(patientDataFile);
            } else {
                logger.debug("Retrieving patient data file");
                context.commit("setPatientDataFileRequested", params.fileId);
                patientDataService
                    .getFile(params.hdid, params.fileId)
                    .then((data) => {
                        context.commit("setPatientDataFile", {
                            fileId: params.fileId,
                            file: data,
                        });
                        resolve(data);
                    })
                    .catch((error: ResultError) => {
                        context.dispatch("handleError", {
                            error,
                            errorType: ErrorType.Retrieve,
                            fileId: params.fileId,
                        });
                        reject(error);
                    });
            }
        });
    },
    retrievePatientData(
        context,
        params: { hdid: string; patientDataTypes: PatientDataType[] }
    ): Promise<PatientData> {
        const logger = container.get<ILogger>(SERVICE_IDENTIFIER.Logger);
        const patientDataService = container.get<IPatientDataService>(
            SERVICE_IDENTIFIER.PatientDataService
        );

        return new Promise((resolve, reject) => {
            if (
                getPatientDataRecordState(context.state, params.hdid).status ===
                LoadStatus.LOADED
            ) {
                logger.debug("Patient data found stored, not querying!");
                const patientData: PatientData = context.getters.patientData(
                    params.hdid
                );
                resolve(patientData);
            } else {
                logger.debug("Retrieving patient data");
                context.commit("setPatientDataRequested", params.hdid);
                patientDataService
                    .getPatientData(params.hdid, params.patientDataTypes)
                    .then((data: PatientData) => {
                        context.commit("setPatientData", {
                            hdid: params.hdid,
                            patientData: data,
                        });
                        resolve(data);
                    })
                    .catch((error: ResultError) => {
                        context.dispatch("handleError", {
                            error,
                            errorType: ErrorType.Retrieve,
                            hdid: params.hdid,
                        });
                        reject(error);
                    });
            }
        });
    },
    handleError(
        context,
        params: {
            error: ResultError;
            errorType: ErrorType;
            hdid?: string;
            fileId: string;
        }
    ): void {
        const logger = container.get<ILogger>(SERVICE_IDENTIFIER.Logger);

        logger.error(`ERROR: ${JSON.stringify(params.error)}`);
        if (params.fileId) {
            context.commit("setPatientDataFileError", {
                fileId: params.fileId,
                error: params.error,
            });
        } else {
            context.commit("setPatientDataError", {
                hdid: params.hdid,
                error: params.error,
            });
        }

        if (params.error.statusCode === 429) {
            context.dispatch(
                "errorBanner/setTooManyRequestsWarning",
                { key: "page" },
                { root: true }
            );
        } else {
            context.dispatch(
                "errorBanner/addError",
                {
                    errorType: params.errorType,
                    source: ErrorSourceType.OrganDonorRegistration,
                },
                { root: true }
            );
        }
    },
};