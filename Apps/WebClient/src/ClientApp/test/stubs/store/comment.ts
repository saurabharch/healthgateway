import { Dictionary } from "@/models/baseTypes";
import { LoadStatus } from "@/models/storeOperations";
import { UserComment } from "@/models/userComment";
import {
    CommentActions,
    CommentGetters,
    CommentModule,
    CommentMutations,
    CommentState,
} from "@/store/modules/comment/types";

var commentState: CommentState = {
    profileComments: {},
    statusMessage: "",
    status: LoadStatus.NONE,
};

var commentGetters: CommentGetters = {
    comments(): Dictionary<UserComment[]> {
        return {};
    },
    getEntryComments: () => (): UserComment[] | null => {
        return null;
    },
    entryHasComments: () => (): boolean => {
        return false;
    },
    isLoading(): boolean {
        return false;
    },
};

var commentActions: CommentActions = {
    retrieve(): Promise<void> {
        return new Promise(() => {});
    },
    createComment(): Promise<UserComment | undefined> {
        return new Promise(() => {});
    },
    updateComment(): Promise<UserComment> {
        return new Promise(() => {});
    },
    deleteComment(): Promise<void> {
        return new Promise(() => {});
    },
    handleError(): void {},
};

var commentMutations: CommentMutations = {
    setRequested(): void {},
    setProfileComments(): void {},
    addComment(): void {},
    updateComment(): void {},
    deleteComment(): void {},
    commentError(): void {},
};

var commentStub: CommentModule = {
    namespaced: true,
    state: commentState,
    getters: commentGetters,
    actions: commentActions,
    mutations: commentMutations,
};

export default commentStub;