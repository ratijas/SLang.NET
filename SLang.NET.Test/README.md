# SLang.NET tests runner

This module is designed to run all-at-once or a particular set of test cases
for SLang.NET compiler.

## Usage

To run all tests execute:

`$ SLang.NET.Test`

To run certain tests:

`$ SLang.NET.Test <test-case> ...`

For example:

`$ SLang.NET.Test declare-unit call-static-routine-return-integer`

For more information try this:

`$ SLang.NET.Test --help`

Report will be generated and printed on standard output in plain text format.

### Exit Code

 * 0: OK
 * 1: One or more tests have failed
 * 2: Command line arguments error

## Structure

Test runner executes _test cases_. Test cases contain source code to compile and
metadata which describes expected compiler behavior. Not all tests are designed
to successfully compile -- crash tests are also an essential to ensure correct
behavior.

`TestsRepository` class helps locate and load test cases. All test cases are
located in a single directory passed in with `--testRootDir=<path>` option. By
default parent directories are searched recursively for the "tests" directory.

## Meta data

Default metadata values are:

```json
{
  "skip": false,
  "stages": {
    "parser": {
      "pass": true,
      "error": ""
    },
    "compiler": {
      "pass": true,
      "error": ""
    },
    "peverify": {
      "pass": true,
      "error": ""
    },
    "run": {
      "run": true,
      "args": [],
      "exit_code": 0,
      "output": "",
      "error": "",
      "timeout": 10
    }
  }
}
```

Tests marked with `"skip": true` won't be run at all.

`error` and `output` values are special string matching objects.

String matching object formats:

 1. Literal AS IS
    ```json
    { "error": "TypeMismatch" }
    ```
 2. Regular expression
    ```json
    {
      "error": {
        "regex": "actual type is Int(32|64)"
      }
    }
    ```
 3. Ignore (always matches)
    ```json
    {
      "error": {
        "ignore": true
      }
    }
    ```

### Stages

Compilation is a process of executing several steps (stages) in particular
order. Should one of them fail, the process is terminated. Each stage defines
expected behavior of  corresponding component.

#### Parser

Parser may pass or fail with specific error message.

#### Compiler

Compiler may pass or fail with specific error message.

#### PeVerify

`peverify` may pass or fail with specific error message.

#### Run

Run compiled program with given arguments. Exit code, output and error output
must match. If program does not manage to finish with `timeout` seconds, it
will be killed.

To skip this stage set `run` flag to `false`.
