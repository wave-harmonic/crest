// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// MACROS - Macros to help with cross pipeline development

#ifndef CREST_MACROS_H
#define CREST_MACROS_H

// Adapted from PPv2 stack. These macros aren't defined for compute shaders.
#ifndef CBUFFER_START
#if defined(SHADER_API_VULKAN) || defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12) || defined(SHADER_API_METAL) || defined(SHADER_API_SWITCH) || defined(SHADER_API_XBOXONE)
    #define CBUFFER_START(name) cbuffer name {
    #define CBUFFER_END };
#elif defined(SHADER_API_PSSL)
    #define CBUFFER_START(name) ConstantBuffer name {
    #define CBUFFER_END };
#else
    #define CBUFFER_START(name)
    #define CBUFFER_END
#endif
#endif

#endif // CREST_MACROS_H
