ASSEMBLY_NAME = Hal
ASSEMBLY = $(ASSEMBLY_NAME).dll

haldir = $(pkglibdir)
hal_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
	$(srcdir)/Manager.cs \
	$(srcdir)/Device.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) -debug -target:library -out:$@ $(VMXMAN_LIBS) -r:$(top_builddir)/dbus-sharp/NDesk.DBus.dll $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)
CLEANFILES = $(hal_SCRIPTS) *.pidb
MAINTAINERCLEANFILES = Makefile.in

