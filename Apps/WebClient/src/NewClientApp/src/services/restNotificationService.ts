import { ServiceCode } from "@/constants/serviceCodes";
import { ExternalConfiguration } from "@/models/configData";
import { HttpError } from "@/models/errors";
import Notification from "@/models/notification";
import {
    IHttpDelegate,
    ILogger,
    INotificationService,
} from "@/services/interfaces";
import ErrorTranslator from "@/utility/errorTranslator";

export class RestNotificationService implements INotificationService {
    private readonly NOTIFICATION_BASE_URI: string = "Notification";
    private logger;
    private http;
    private baseUri;
    private isEnabled;

    constructor(
        logger: ILogger,
        http: IHttpDelegate,
        config: ExternalConfiguration
    ) {
        this.logger = logger;
        this.http = http;
        this.isEnabled =
            config.webClient.featureToggleConfiguration.notificationCentre.enabled;
        this.baseUri = config.serviceEndpoints["GatewayApi"];
    }

    getNotifications(hdid: string): Promise<Notification[]> {
        return new Promise((resolve, reject) => {
            if (!this.isEnabled) {
                resolve([]);
                return;
            }

            this.http
                .getWithCors<Notification[]>(
                    `${this.baseUri}${this.NOTIFICATION_BASE_URI}/${hdid}`
                )
                .then((result) => resolve(result))
                .catch((err: HttpError) => {
                    this.logger.error(
                        `Error in RestNotificationService.getNotifications()`
                    );
                    return reject(
                        ErrorTranslator.internalNetworkError(
                            err,
                            ServiceCode.HealthGatewayUser
                        )
                    );
                });
        });
    }
    dismissNotification(hdid: string, notificationId: string): Promise<void> {
        return new Promise((resolve, reject) => {
            if (!this.isEnabled) {
                resolve();
                return;
            }

            this.http
                .delete<void>(
                    `${this.baseUri}${this.NOTIFICATION_BASE_URI}/${hdid}/${notificationId}`,
                    notificationId
                )
                .then(() => resolve())
                .catch((err: HttpError) => {
                    this.logger.error(
                        `Error in RestNotificationService.dismissNotification()`
                    );
                    return reject(
                        ErrorTranslator.internalNetworkError(
                            err,
                            ServiceCode.HealthGatewayUser
                        )
                    );
                });
        });
    }
    dismissNotifications(hdid: string): Promise<void> {
        return new Promise((resolve, reject) => {
            if (!this.isEnabled) {
                resolve();
                return;
            }

            this.http
                .delete<void>(
                    `${this.baseUri}${this.NOTIFICATION_BASE_URI}/${hdid}`,
                    hdid
                )
                .then(() => resolve())
                .catch((err: HttpError) => {
                    this.logger.error(
                        `Error in RestNotificationService.dismissNotifications()`
                    );
                    return reject(
                        ErrorTranslator.internalNetworkError(
                            err,
                            ServiceCode.HealthGatewayUser
                        )
                    );
                });
        });
    }
}