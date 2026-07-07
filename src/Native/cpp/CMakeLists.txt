cmake_minimum_required(VERSION 3.22)
project(stockfish_lite CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Stabilne i proverene optimizacije brzine za mobilne čipove
add_compile_options(-O3 -fno-exceptions -funroll-loops)

# Definišemo makroe za ugradnju mreže unutar samog fajla
add_compile_definitions(
    IS_64BIT
    USE_NEON=1
    EVAL_EMBEDDING=1
    EMBED_BUILT_IN_NET=1
)

# Skupljamo sav kôd iz strukture Stockfish-a
file(GLOB_RECURSE STOCKFISH_SOURCES
    "*.cpp"
    "*.h"
)

# META 1: Pravimo deljenu biblioteku (izbacuje libstockfish_lite.so)
add_library(stockfish_lite SHARED
    ${STOCKFISH_SOURCES}
)

target_include_directories(stockfish_lite PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/nnue
    ${CMAKE_CURRENT_SOURCE_DIR}/syzygy
)

target_link_libraries(stockfish_lite
    android
    log
)

# META 2: Pravimo samostalni izvršni program za Process.Start (izbacuje stockfish_android)
add_executable(stockfish_android
    ${STOCKFISH_SOURCES}
)

target_include_directories(stockfish_android PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/nnue
    ${CMAKE_CURRENT_SOURCE_DIR}/syzygy
)

target_link_libraries(stockfish_android
    android
    log
)
