The idea is to demonstrate routines calling each other and conditionally returning values.

In pseudo-code it would be like:

```c
int foo(int x) {
    if (x) {
        return 42;
    } else {
        return bar();
    }
}

int bar() {
    return foo(1);
}

main() {
    return foo(0);
}
```