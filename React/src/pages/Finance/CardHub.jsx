import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Table,
  Typography,
  Statistic,
  Popconfirm,
  ColorPicker,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, ThunderboltOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import { financeApi, EXPENSE_CREATION_SOURCE, normalizeCreditCardPayload } from '@services/financeApi';
import { countWeekdaysInMonth } from '@/utils/businessDays';

dayjs.locale('pt-br');

const { Title, Text } = Typography;

const emptyGuid = '00000000-0000-0000-0000-000000000000';
const STORAGE_CARD = 'framework:selectedCreditCardId';

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

const CardHub = () => {
  const { message } = App.useApp();
  const [cards, setCards] = useState([]);
  const [accounts, setAccounts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [selectedCardId, setSelectedCardId] = useState(null);
  const [invoiceMonth, setInvoiceMonth] = useState(() => dayjs().startOf('month'));
  const [invoice, setInvoice] = useState(null);
  const [loading, setLoading] = useState(false);
  const [invoiceLoading, setInvoiceLoading] = useState(false);
  const [modal, setModal] = useState({ open: false, record: null });
  const [form] = Form.useForm();
  const [launchForm] = Form.useForm();
  const modalCardKind = Form.useWatch('cardKind', form);
  const modalMealDaily = Form.useWatch('mealVoucherDailyAmount', form);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [cc, acc, cats] = await Promise.all([
        financeApi.creditCards(),
        financeApi.accounts(),
        financeApi.categories(),
      ]);
      setCards(cc || []);
      setAccounts(acc || []);
      setCategories((cats || []).filter((c) => c.isExpense));
    } catch (e) {
      message.error(e.message || 'Erro ao carregar dados');
    } finally {
      setLoading(false);
    }
  }, [message]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  useEffect(() => {
    const stored = sessionStorage.getItem(STORAGE_CARD);
    if (stored) setSelectedCardId(stored);
  }, []);

  useEffect(() => {
    if (selectedCardId) sessionStorage.setItem(STORAGE_CARD, selectedCardId);
    else sessionStorage.removeItem(STORAGE_CARD);
  }, [selectedCardId]);

  useEffect(() => {
    if (!selectedCardId) {
      setInvoice(null);
      return undefined;
    }
    let cancelled = false;
    (async () => {
      setInvoiceLoading(true);
      try {
        const y = invoiceMonth.year();
        const m = invoiceMonth.month() + 1;
        const inv = await financeApi.invoice(selectedCardId, y, m);
        if (!cancelled) setInvoice(inv);
      } catch (e) {
        if (!cancelled) {
          message.error(e.message || 'Erro ao carregar fatura');
          setInvoice(null);
        }
      } finally {
        if (!cancelled) setInvoiceLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedCardId, invoiceMonth, message]);

  const selectedCard = useMemo(
    () => (cards || []).find((c) => c.id === selectedCardId) || null,
    [cards, selectedCardId]
  );

  const openModal = (record = null) => {
    setModal({ open: true, record });
    form.resetFields();
    if (record) {
      form.setFieldsValue({
        ...record,
        cardKind: record.isMealVoucher ? 'meal' : 'credit',
      });
    } else {
      form.setFieldsValue({ id: emptyGuid, cardKind: 'credit', closingDay: 10, dueDay: 15 });
    }
  };

  const submitCard = async () => {
    const v = await form.validateFields();
    setLoading(true);
    try {
      const saved = await financeApi.creditCardUpsert(normalizeCreditCardPayload(v));
      message.success('Cartão salvo');
      setModal({ open: false, record: null });
      await loadAll();
      if (saved?.id) setSelectedCardId(saved.id);
    } catch (e) {
      if (e?.errorFields) return;
      message.error(e.message || 'Erro ao salvar');
    } finally {
      setLoading(false);
    }
  };

  const onLaunchExpense = async (values) => {
    if (!selectedCardId) {
      message.warning('Selecione um cartão no filtro acima');
      return;
    }
    setLoading(true);
    try {
      await financeApi.expenseUpsert({
        id: emptyGuid,
        amount: values.amount,
        date: values.date?.toDate?.() ? values.date.toDate().toISOString() : new Date().toISOString(),
        categoryId: values.categoryId,
        description: values.description || 'Gasto no cartão',
        paymentMethod: 2,
        storeLocation: null,
        creditCardId: selectedCardId,
        installmentPlanId: null,
        imagePath: null,
        creationSource: EXPENSE_CREATION_SOURCE.UNSPECIFIED,
      });
      message.success('Lançamento registrado neste cartão');
      launchForm.resetFields();
      launchForm.setFieldsValue({ date: dayjs() });
      const y = invoiceMonth.year();
      const m = invoiceMonth.month() + 1;
      const inv = await financeApi.invoice(selectedCardId, y, m);
      setInvoice(inv);
    } catch (e) {
      message.error(e.message || 'Erro ao lançar');
    } finally {
      setLoading(false);
    }
  };

  const ccCols = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    {
      title: 'Tipo',
      key: 'tipo',
      width: 140,
      render: (_, r) => (r.isMealVoucher ? 'Vale alimentação' : 'Crédito'),
    },
    {
      title: 'Fechamento',
      dataIndex: 'closingDay',
      key: 'closingDay',
      render: (v, r) => (r.isMealVoucher ? '—' : v),
    },
    {
      title: 'Vencimento',
      dataIndex: 'dueDay',
      key: 'dueDay',
      render: (v, r) => (r.isMealVoucher ? '—' : v),
    },
    {
      title: 'Ações',
      key: 'a',
      width: 160,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openModal(r)}>
            Editar
          </Button>
          <Popconfirm title="Remover este cartão?" onConfirm={() => financeApi.creditCardDelete(r.id).then(loadAll)}>
            <Button size="small" danger icon={<DeleteOutlined />}>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const invoiceCols = [
    { title: 'Data', dataIndex: 'date', key: 'date', render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—') },
    { title: 'Descrição', dataIndex: 'description', key: 'description', ellipsis: true },
    { title: 'Categoria', dataIndex: 'categoryName', key: 'categoryName' },
    { title: 'Valor', dataIndex: 'amount', key: 'amount', align: 'right', render: (v) => formatMoney(v) },
  ];

  const accCols = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    {
      title: 'Saldo',
      dataIndex: 'balance',
      key: 'balance',
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  return (
    <div>
      <Title level={3} style={{ marginTop: 0 }}>
        Cartões
      </Title>
      <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        Escolha um cartão para filtrar a fatura e lançar despesas vinculadas a ele. As contas abaixo servem de referência
        para pagamento (cadastro em Cadastros).
      </Text>

      <Card style={{ marginBottom: 16 }} loading={loading && cards.length === 0}>
        <Row gutter={[12, 12]} align="middle">
          <Col xs={24} md={14}>
            <Text strong style={{ display: 'block', marginBottom: 8 }}>
              Cartão ativo (filtro)
            </Text>
            <Select
              allowClear
              placeholder="Selecione um cartão para ver itens e lançar"
              style={{ width: '100%' }}
              value={selectedCardId}
              onChange={(v) => setSelectedCardId(v || null)}
              options={cards.map((c) => ({ value: c.id, label: c.name }))}
              showSearch
              optionFilterProp="label"
            />
          </Col>
          <Col xs={24} md={10} style={{ textAlign: 'right' }}>
            <Space wrap>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal(null)}>
                Novo cartão
              </Button>
              <Link to={selectedCardId ? `/lancamento?cartao=${selectedCardId}` : '/lancamento'}>
                <Button icon={<ThunderboltOutlined />} disabled={!selectedCardId}>
                  Lançamento rápido
                </Button>
              </Link>
            </Space>
          </Col>
        </Row>
        {selectedCard && (
          <div style={{ marginTop: 12 }}>
            <Text type="secondary">
              {selectedCard.isMealVoucher ? (
                <>
                  Vale alimentação · {formatMoney(selectedCard.mealVoucherDailyAmount || 0)} por dia útil · Crédito todo
                  dia {selectedCard.mealVoucherCreditDay ?? '—'} do mês
                </>
              ) : (
                <>
                  Fechamento dia {selectedCard.closingDay} · Vencimento dia {selectedCard.dueDay}
                </>
              )}
            </Text>
          </div>
        )}
      </Card>

      <Card title="Cadastro de cartões" style={{ marginBottom: 16 }}>
        <Table
          rowKey="id"
          columns={ccCols}
          dataSource={cards}
          pagination={false}
          scroll={{ x: true }}
          locale={{ emptyText: 'Nenhum cartão. Clique em Novo cartão.' }}
        />
      </Card>

      <Card
        title="Itens da fatura (filtrados pelo cartão)"
        extra={
          <DatePicker
            picker="month"
            value={invoiceMonth}
            onChange={(v) => v && setInvoiceMonth(v.startOf('month'))}
            format="MM/YYYY"
            disabled={!selectedCardId}
          />
        }
      >
        {!selectedCardId ? (
          <Text type="secondary">Selecione um cartão para listar os lançamentos do período.</Text>
        ) : (
          <>
            <Statistic title="Total no período" value={invoice?.total ?? 0} formatter={formatMoney} />
            {invoice?.isMealVoucher && (
              <>
                <Divider style={{ margin: '16px 0' }} />
                <Row gutter={[16, 16]}>
                  <Col xs={24} sm={12} md={8}>
                    <Statistic
                      title="Dias úteis no mês"
                      value={invoice.businessDaysInMonth ?? 0}
                    />
                  </Col>
                  <Col xs={24} sm={12} md={8}>
                    <Statistic
                      title="Valor por dia útil"
                      value={invoice.mealVoucherDailyAmount ?? 0}
                      formatter={formatMoney}
                    />
                  </Col>
                  <Col xs={24} sm={12} md={8}>
                    <Statistic
                      title="Crédito previsto (dia útil × dias do mês)"
                      value={invoice.expectedMonthlyCredit ?? 0}
                      formatter={formatMoney}
                    />
                  </Col>
                </Row>
                <Text type="secondary" style={{ display: 'block', marginTop: 12 }}>
                  O benefício é creditado no dia {invoice.mealVoucherCreditDay ?? '—'} de cada mês. O total exibido é o
                  valor fixo diário multiplicado pelos dias úteis (segunda a sexta) do mês selecionado.
                </Text>
              </>
            )}
            <Divider style={{ margin: '16px 0' }} />
            <Table
              rowKey="id"
              loading={invoiceLoading}
              dataSource={invoice?.expenses || []}
              columns={invoiceCols}
              pagination={{ pageSize: 8 }}
              scroll={{ x: true }}
              locale={{ emptyText: 'Nenhum item neste mês para este cartão' }}
            />
          </>
        )}
      </Card>

      <Modal
        open={modal.open}
        title={modal.record ? 'Editar cartão' : 'Novo cartão'}
        onCancel={() => setModal({ open: false, record: null })}
        onOk={submitCard}
        confirmLoading={loading}
        destroyOnClose
        width={480}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="id" hidden>
            <Input />
          </Form.Item>
          <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
            <Input placeholder="Ex.: Nubank, Alelo Alimentação" />
          </Form.Item>
          <Form.Item name="cardKind" label="Tipo" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 'credit', label: 'Cartão de crédito' },
                { value: 'meal', label: 'Vale alimentação' },
              ]}
            />
          </Form.Item>
          {modalCardKind === 'meal' ? (
            <>
              <Form.Item
                name="mealVoucherDailyAmount"
                label="Valor fixo por dia útil"
                rules={[
                  { required: true, message: 'Informe o valor por dia útil' },
                  { type: 'number', min: 0.01, message: 'Valor deve ser maior que zero' },
                ]}
                extra="Usado para calcular o crédito do mês: este valor × quantidade de dias úteis (seg–sex)."
              >
                <InputNumber prefix="R$" min={0.01} style={{ width: '100%' }} controls={false} />
              </Form.Item>
              <Form.Item
                name="mealVoucherCreditDay"
                label="Dia do crédito no mês"
                rules={[{ required: true, message: 'Informe o dia (1–31)' }]}
                extra="Dia em que o benefício é creditado (referência; o valor do mês segue os dias úteis totais)."
              >
                <InputNumber min={1} max={31} style={{ width: '100%' }} />
              </Form.Item>
              {modalMealDaily != null && Number(modalMealDaily) > 0 && (
                <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
                  Exemplo no mês da fatura ({invoiceMonth.format('MM/YYYY')}):{' '}
                  {countWeekdaysInMonth(invoiceMonth.year(), invoiceMonth.month() + 1)} dias úteis ×{' '}
                  {formatMoney(modalMealDaily)} ={' '}
                  <strong>
                    {formatMoney(
                      Number(modalMealDaily) *
                        countWeekdaysInMonth(invoiceMonth.year(), invoiceMonth.month() + 1)
                    )}
                  </strong>
                </Text>
              )}
            </>
          ) : (
            <>
              <Form.Item name="closingDay" label="Dia de fechamento" rules={[{ required: true }]}>
                <InputNumber min={1} max={31} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="dueDay" label="Dia de vencimento" rules={[{ required: true }]}>
                <InputNumber min={1} max={31} style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}
          <Form.Item
            name="themeColor"
            label="Cor de identificação"
            extra="Aparece no dashboard ao filtrar por cartão."
            getValueFromEvent={(val) => {
              if (val == null) return undefined;
              if (typeof val.toHexString === 'function') return val.toHexString();
              return val;
            }}
          >
            <ColorPicker showText format="hex" allowClear style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default CardHub;
