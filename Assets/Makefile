MODNAME		:= CircBurn
KSPDIR		:= ${HOME}/ksp/KSP_linux
GAMEDATA	:= ${KSPDIR}/GameData
MODGAMEDATA	:= ${GAMEDATA}/${MODNAME}

RESGEN2		:= resgen2
GMCS		:= gmcs
GMCSFLAGS	:= -optimize -warnaserror
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: ${MODNAME}_icon.png
#	${GUI_ICONS}

info:
	@echo "${MODNAME} Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

${MODNAME}_icon.png: ${MODNAME}_icon.svg
	inkscape --export-width=24 --export-height=24 --export-background='#000000' --export-background-opacity=0 --export-png=$@ $^

clean:
	rm -f ${MODNAME}_icon.png

install: all
	mkdir -p ${MODGAMEDATA}/Textures
	cp ${MODNAME}_icon.png ${MODGAMEDATA}/Textures

.PHONY: all clean install
