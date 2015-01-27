/* AXML Parser
 * https://github.com/claudxiao/AndTools
 * Claud Xiao <iClaudXiao@gmail.com>
 */
#ifndef AXMLPARSER_H
#define AXMLPARSER_H

#include "../stdint.h"

typedef enum{
	AE_STARTDOC = 0,
	AE_ENDDOC,
	AE_STARTTAG,
	AE_ENDTAG,
	AE_TEXT,
	AE_ERROR,
} AxmlEvent_t;

#ifdef __cplusplus
#if __cplusplus
extern "C" {
#endif
#endif

__declspec(dllexport) void *AxmlOpen(char *buffer, size_t size);

__declspec(dllexport) AxmlEvent_t AxmlNext(void *axml);

__declspec(dllexport) char *AxmlGetTagPrefix(void *axml);
__declspec(dllexport) char *AxmlGetTagName(void *axml);

__declspec(dllexport) int AxmlNewNamespace(void *axml);
__declspec(dllexport) char *AxmlGetNsPrefix(void *axml);
__declspec(dllexport) char *AxmlGetNsUri(void *axml);

__declspec(dllexport) uint32_t AxmlGetAttrCount(void *axml);
__declspec(dllexport) char *AxmlGetAttrPrefix(void *axml, uint32_t i);
__declspec(dllexport) char *AxmlGetAttrName(void *axml, uint32_t i);
__declspec(dllexport) char *AxmlGetAttrValue(void *axml, uint32_t i);

__declspec(dllexport) char *AxmlGetText(void *axml);

__declspec(dllexport) int AxmlClose(void *axml);

__declspec(dllexport) int AxmlToXml(char **outbuf, size_t *outsize, char *inbuf, size_t insize);

#ifdef __cplusplus
#if __cplusplus
};
#endif
#endif

#endif
