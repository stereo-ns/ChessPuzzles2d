#include <iostream>
#include <sstream>
#include <string>
#include <algorithm>
#include <cstring>
#include "uci.h"
#include "types.h"

extern "C" {

    static std::stringstream engineOutputBuffer;
    
    // Koristimo fabrički stringstream koji glumi standardni ulaz za Stockfish
    static std::stringstream engineInputBuffer;

    void send_stockfish_command(const char* command) {
        if (!command) return;
        
        // Upisujemo komandu u naš kontrolisani RAM bafer i dodajemo novi red
        engineInputBuffer << command << "\n";
    }

    int get_stockfish_output(char* outBuffer, int maxLen) {
        std::string currentData = engineOutputBuffer.str();
        if (currentData.empty()) {
            return 0;
        }

        int bytesToWrite = std::min(maxLen - 1, (int)currentData.length());
        std::strncpy(outBuffer, currentData.c_str(), bytesToWrite);
        outBuffer[bytesToWrite] = '\0';

        engineOutputBuffer.str("");
        engineOutputBuffer.clear();
        return bytesToWrite;
    }

    void init_stockfish_engine() {
        // 1. Preusmeravamo standardni C++ izlaz (cout) u naš RAM izlazni bafer
        std::cout.rdbuf(engineOutputBuffer.rdbuf());

        // 2. Preusmeravamo standardni C++ ulaz (cin) u naš bezbedni RAM ulazni bafer
        std::cin.rdbuf(engineInputBuffer.rdbuf());

        int argc = 1;
        char* argv[] = {(char*)"stockfish", nullptr};
        
        // Sada možemo bezbedno da pokrenemo zvaničnu petlju!
        // Pošto std::cin više ne blokira niti, već čita iz našeg RAM-a, Android neće ugasiti aplikaciju
        Stockfish::UCIEngine uci(argc, argv);
        uci.loop();
    }
}

