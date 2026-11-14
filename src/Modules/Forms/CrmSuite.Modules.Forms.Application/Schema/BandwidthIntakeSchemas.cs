namespace CrmSuite.Modules.Forms.Application.Schema;

public static class BandwidthIntakeSchemas
{
    public const string Request = """
    {"fields":[
      {"key":"cooperation_type","label":"نوع همکاری","type":"select","required":true,"order":1,"options":[{"value":"invoice","label":"فاکتوری"},{"value":"contract","label":"قراردادی"}]},
      {"key":"billing_period_months","label":"دوره صدور فاکتور","type":"select","required":true,"order":2,"options":[{"value":"1","label":"ماهیانه"},{"value":"3","label":"سه ماهه"},{"value":"6","label":"شش ماهه"},{"value":"12","label":"یکساله"}]},
      {"key":"bandwidth_mb","label":"میزان پهنای باند (مگابیت)","type":"number","required":true,"min":1,"order":3},
      {"key":"extra_ip_count","label":"تعداد IP اضافی","type":"number","required":true,"min":0,"order":4},
      {"key":"substrate","label":"نوع بستر","type":"select","required":true,"order":5,"options":[{"value":"wireless","label":"وایرلس"},{"value":"fiber","label":"فیبر"},{"value":"cable","label":"کابلی"}]},
      {"key":"building_height","label":"ارتفاع ساختمان / تعداد طبقه","type":"number","required":true,"min":0,"order":6,"visibleIf":{"field":"substrate","equals":"wireless"}},
      {"key":"geo_coordinates","label":"مختصات جغرافیایی","type":"text","required":true,"order":7,"visibleIf":{"field":"substrate","equals":"wireless"}},
      {"key":"tower_height","label":"ارتفاع دکل موجود (متر)","type":"number","required":true,"min":0,"order":8,"visibleIf":{"field":"substrate","equals":"wireless"}},
      {"key":"existing_equipment_type","label":"نوع تجهیزات موجود (در صورت وجود)","type":"text","required":false,"order":9,"visibleIf":{"field":"substrate","equals":"wireless"}},
      {"key":"existing_equipment_count","label":"تعداد تجهیزات موجود","type":"number","required":false,"min":0,"order":10,"visibleIf":{"field":"substrate","equals":"wireless"}},
      {"key":"has_cable","label":"کابل دارد؟","type":"select","required":true,"order":11,"options":[{"value":"yes","label":"بله"},{"value":"no","label":"خیر"}],"visibleIf":{"field":"substrate","equals":"wireless"}}
    ]}
    """;

    public const string Decision = """
    {"fields":[
      {"key":"needs_visit","label":"نیاز به امکان‌سنجی حضوری دارد؟","type":"select","required":true,"order":1,"options":[{"value":"yes","label":"بله"},{"value":"no","label":"خیر"}]},
      {"key":"notes","label":"توضیحات ارجاع","type":"textarea","required":false,"order":2}
    ]}
    """;

    public const string Result = """
    {"fields":[
      {"key":"is_feasible","label":"نتیجه امکان‌سنجی","type":"select","required":true,"order":1,"options":[{"value":"yes","label":"مثبت"},{"value":"no","label":"منفی"}]},
      {"key":"required_equipment","label":"نوع تجهیزات مورد نیاز","type":"textarea","required":true,"order":2},
      {"key":"needs_tower","label":"نیاز به دکل دارد؟","type":"select","required":true,"order":3,"options":[{"value":"yes","label":"بله"},{"value":"no","label":"خیر"}]},
      {"key":"tower_height","label":"ارتفاع دکل مورد نیاز (متر)","type":"number","required":true,"min":0.1,"order":4,"visibleIf":{"field":"needs_tower","equals":"yes"}},
      {"key":"pipe_meters","label":"متراژ لوله مورد نیاز (در صورت عدم نیاز صفر)","type":"number","required":true,"min":0,"order":5},
      {"key":"cable_meters","label":"متراژ کابل","type":"number","required":true,"min":0,"order":6},
      {"key":"description","label":"توضیحات","type":"textarea","required":false,"order":7},
      {"key":"attachment","label":"عکس امکان‌سنجی","type":"file","required":false,"order":8}
    ]}
    """;
}
