//const CompressionPlugin = require("compression-webpack-plugin");
module.exports = {
    productionSourceMap: false,
    lintOnSave: false,
    integrity: true,
    devServer: {
        overlay: {
            warnings: true,
            errors: true,
        },
    },
    chainWebpack: (config) => {
        config.plugins.delete("split-manifest").delete("inline-manifest");
        //config.plugin("CompressionPlugin").use(CompressionPlugin);
        config.resolve.symlinks(false);

        /**
         * Disable (or customize) prefetch, see:
         * https://cli.vuejs.org/guide/html-and-static-assets.html#prefetch
         */
        config.plugins.delete("prefetch");

        /**
         * Configure preload to load all chunks
         * NOTE: use `allChunks` instead of `all` (deprecated)
         */
        config.plugin("preload").tap((options) => {
            options[0].include = "allChunks";
            return options;
        });
    },
    pwa: {
        name: "HG PWA Test",
        themeColor: "#4DBA87",
        msTileColor: "#000000",
        appleMobileWebAppCapable: "yes",
        appleMobileWebAppStatusBarStyle: "black",

        // configure the workbox plugin
        workboxPluginMode: "InjectManifest",
        workboxOptions: {
            // swSrc is required in InjectManifest mode.
            swSrc: "public/sw.js",
            // ...other Workbox options...
        },
    },
};
