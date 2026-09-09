/* Independent native x86 arithmetic probe; never loads an AO DLL. */
#include <stdio.h>
#include <stdint.h>
int main(void) {
    const int64_t inputs[] = {1000,16000,16667,33333,1000000,3500000};
    int64_t frequency = 1000000;
    double scale = 1000.0;
    unsigned short saved, extended = 0x037f, truncate = 0x0f7f;
    unsigned int i, mode;
    const unsigned short modes[] = {0x007f, 0x027f, 0x037f};
    __asm { fnstcw saved }
    puts("controlWord,microseconds,nativeMilliseconds");
    for (mode=0; mode<3; ++mode) {
    extended = modes[mode]; truncate = extended | 0x0c00;
    for (i=0; i<sizeof(inputs)/sizeof(inputs[0]); ++i) {
        int64_t input = inputs[i], result = 0;
        __asm {
            fldcw extended
            fild input
            fild frequency
            fdivp st(1), st(0)
            fmul scale
            fldcw truncate
            fistp result
            fldcw saved
        }
        printf("%u,%lld,%lld\n", extended, input, result);
    }
    }
    return 0;
}
