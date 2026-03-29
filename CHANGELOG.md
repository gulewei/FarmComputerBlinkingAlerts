# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-03-29

### Fixed
- Cask detection now only alerts when products reach iridium quality (quality >= 4)
- Followed SMAPI best practices for NetInt field access (use Quality property)

### Note
- Code for animal products detection, fruit trees detection, and crab pots detection has been implemented but is currently commented out in the `ScanAll()` method. Uncomment lines 42-44 in FarmScanner.cs to enable these features.

## [1.0.0] - 2026-03-19

### Added
- Initial release
- Daily farm scanning for:
  - Ready-to-harvest crops
  - Finished machines
  - Empty hay in silo
- Pulsing aura visual effect around Farm Computer
- Interaction detection to stop pulsing
- Basic logging for debugging