cmake_minimum_required(VERSION 3.15)
project(DotnetRunTests NONE)

find_program(DOTNET_EXE NAMES dotnet)
if(NOT DOTNET_EXE)
  message(FATAL_ERROR "dotnet not found in PATH. Установите .NET SDK и убедитесь, что 'dotnet' доступен.")
endif()

set(SOLUTION_DIR "${CMAKE_SOURCE_DIR}")

add_custom_target(dotnet_build
    COMMAND ${DOTNET_EXE} build "${SOLUTION_DIR}" --configuration Debug
    WORKING_DIRECTORY "${SOLUTION_DIR}"
    COMMENT "dotnet build (обычная сборка)"
)

add_custom_target(dotnet_build_tests
    COMMAND ${DOTNET_EXE} build "${SOLUTION_DIR}" --configuration Debug -p:OutputType=Library
    WORKING_DIRECTORY "${SOLUTION_DIR}"
    COMMENT "dotnet build (overriding OutputType=Library)"
)

add_custom_target(run_tests
    DEPENDS dotnet_build_tests
    COMMAND ${DOTNET_EXE} test "${SOLUTION_DIR}" --no-build --logger "console;verbosity=minimal"
    WORKING_DIRECTORY "${SOLUTION_DIR}"
    COMMENT "Запуск unit-тестов (dotnet test)"
)
