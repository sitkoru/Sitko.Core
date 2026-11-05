module.exports = {
    "extends": "@sitkoru/semantic-release-config",
    tagFormat: "${version}",
    branches: [
        "+([0-9])?(.{+([0-9]),x}).x",
        "master",
        "main",
        "next",
        "next-major",
        { name: "v13", channel: "beta", prerelease: "beta" },
        { name: "alpha", prerelease: true }
    ]
};
