cmake_minimum_required(VERSION 2.8)

project(Graphics C)

add_executable(graphics main.c)

set_property(TARGET graphics PROPERTY C_STANDARD 11)
set_property(TARGET graphics PROPERTY C_STANDARD_REQUIRED ON)

target_link_libraries(graphics m SDL2)
