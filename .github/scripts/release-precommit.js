'use strict';

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const VERSION_FILE = path.resolve(process.env.GITHUB_WORKSPACE || process.cwd(), 'version.json');
const SEMVER = /^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$/;

exports.preCommit = ({ tag, version }) => {
  if (!SEMVER.test(version)) {
    throw new Error(`Refusing to write invalid release version "${version}".`);
  }

  if (tag !== `v${version}`) {
    throw new Error(`Release tag/version mismatch: tag "${tag}" does not match version "${version}".`);
  }

  const existingTag = spawnSync(
    'git',
    ['rev-parse', '--verify', '--quiet', `refs/tags/${tag}`],
    { encoding: 'utf8' },
  );

  if (existingTag.status === 0) {
    throw new Error(
      `Refusing to create duplicate release tag ${tag}. ` +
      'The release version must be derived from the latest Git tag.',
    );
  }

  // git rev-parse --verify --quiet returns 1 when the ref does not exist.
  if (existingTag.status !== 1) {
    throw new Error(
      `Unable to verify whether release tag ${tag} already exists: ` +
      (existingTag.stderr || `git exited with status ${existingTag.status}`),
    );
  }

  const versionDocument = JSON.parse(fs.readFileSync(VERSION_FILE, 'utf8'));
  versionDocument.version = version;
  fs.writeFileSync(VERSION_FILE, `${JSON.stringify(versionDocument, null, 2)}\n`, 'utf8');

  console.log(`Synchronized version.json to ${version} from release tag ${tag}.`);
};
